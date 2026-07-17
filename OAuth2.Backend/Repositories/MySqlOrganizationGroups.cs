using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Data;
using OAuth2.Options;

namespace OAuth2.Repositories;

internal sealed class MySqlOrganizationGroups(IOptions<MySqlOptions> mysqlOptions)
    : IOrganizationGroups
{
    public async Task<IReadOnlyList<(OrganizationGroup Group, long MemberCount)>?> GetGroupsAsync(
        string organizationId,
        string actorAccountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorAccountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        if (await GetRoleAsync(
                connection,
                null,
                organizationId,
                actorAccountId,
                cancellationToken) is null)
        {
            return null;
        }

        const string QUERY = """
            SELECT
                `g`.`organization_id` AS `OrganizationId`,
                `g`.`id` AS `Id`,
                `g`.`name` AS `Name`,
                `g`.`created_at` AS `CreatedAt`,
                COUNT(`gm`.`account_id`) AS `MemberCount`
            FROM `organization_group` `g`
            LEFT JOIN `organization_group_member` `gm`
                ON `gm`.`organization_id` = `g`.`organization_id`
                AND `gm`.`group_id` = `g`.`id`
            WHERE `g`.`organization_id` = @organizationId
            GROUP BY
                `g`.`organization_id`,
                `g`.`id`,
                `g`.`name`,
                `g`.`created_at`
            ORDER BY `g`.`name`, `g`.`id`
            """;
        var command = new CommandDefinition(
            QUERY,
            new { organizationId },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<OrganizationGroupRow>(command);
        return rows.Select(static row => (row.ToGroup(), row.MemberCount)).ToArray();
    }

    public async Task<(OrganizationGroup Group, long MemberCount)?> GetGroupAsync(
        string organizationId,
        string groupId,
        string actorAccountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorAccountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        if (await GetRoleAsync(
                connection,
                null,
                organizationId,
                actorAccountId,
                cancellationToken) is null)
        {
            return null;
        }

        var row = await GetGroupRowAsync(
            connection,
            null,
            organizationId,
            groupId,
            cancellationToken);
        return row is null ? null : (row.ToGroup(), row.MemberCount);
    }

    public async Task<(OrganizationGroupMutationStatus Status, OrganizationGroup? Group)> AddGroupAsync(
        string organizationId,
        string actorAccountId,
        string groupId,
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (!await LockOrganizationAsync(
                connection,
                transaction,
                organizationId,
                cancellationToken))
        {
            return (OrganizationGroupMutationStatus.OrganizationNotFound, null);
        }

        var actorRole = await GetRoleAsync(
            connection,
            transaction,
            organizationId,
            actorAccountId,
            cancellationToken);
        if (actorRole is null)
        {
            return (OrganizationGroupMutationStatus.OrganizationNotFound, null);
        }

        if (!OrganizationRoles.CanManageGroups(actorRole))
        {
            return (OrganizationGroupMutationStatus.Forbidden, null);
        }

        var group = new OrganizationGroup
        {
            OrganizationId = organizationId,
            Id = groupId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        const string QUERY = """
            INSERT INTO `organization_group`
                (`organization_id`, `id`, `name`, `created_by`, `created_at`)
            VALUES
                (@OrganizationId, @Id, @Name, @actorAccountId, @CreatedAt)
            """;
        var command = new CommandDefinition(
            QUERY,
            new
            {
                group.OrganizationId,
                group.Id,
                group.Name,
                actorAccountId,
                group.CreatedAt
            },
            transaction,
            cancellationToken: cancellationToken);
        try
        {
            if (await connection.ExecuteAsync(command) != 1)
            {
                throw new InvalidOperationException("Failed to create an organization group.");
            }
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            return (OrganizationGroupMutationStatus.GroupExists, null);
        }

        await transaction.CommitAsync(cancellationToken);
        return (OrganizationGroupMutationStatus.Succeeded, group);
    }

    public async Task<(IReadOnlyList<OrganizationMember> Items, long TotalCount)?> GetMembersAsync(
        string organizationId,
        string groupId,
        string actorAccountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorAccountId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        if (await GetRoleAsync(
                connection,
                null,
                organizationId,
                actorAccountId,
                cancellationToken) is null
            || await GetGroupRowAsync(
                connection,
                null,
                organizationId,
                groupId,
                cancellationToken) is null)
        {
            return null;
        }

        const string COUNT_QUERY = """
            SELECT COUNT(*)
            FROM `organization_group_member`
            WHERE `organization_id` = @organizationId
                AND `group_id` = @groupId
            """;
        var command = new CommandDefinition(
            COUNT_QUERY,
            new { organizationId, groupId },
            cancellationToken: cancellationToken);
        var totalCount = await connection.QuerySingleAsync<long>(command);

        const string PAGE_QUERY = """
            SELECT
                `a`.`id` AS `AccountId`,
                `a`.`name` AS `Name`,
                `om`.`role` AS `Role`,
                `gm`.`created_at` AS `JoinedAt`
            FROM `organization_group_member` `gm`
            INNER JOIN `organization_member` `om`
                ON `om`.`organization_id` = `gm`.`organization_id`
                AND `om`.`account_id` = `gm`.`account_id`
            INNER JOIN `account` `a`
                ON `a`.`id` = `gm`.`account_id`
            WHERE `gm`.`organization_id` = @organizationId
                AND `gm`.`group_id` = @groupId
            ORDER BY
                CASE `om`.`role`
                    WHEN 'owner' THEN 1
                    WHEN 'admin' THEN 2
                    ELSE 3
                END,
                `a`.`name`,
                `a`.`id`
            LIMIT @pageSize OFFSET @offset
            """;
        var offset = checked((long)(page - 1) * pageSize);
        command = new CommandDefinition(
            PAGE_QUERY,
            new { organizationId, groupId, pageSize, offset },
            cancellationToken: cancellationToken);
        var members = await connection.QueryAsync<OrganizationMember>(command);
        return (members.ToArray(), totalCount);
    }

    public Task<OrganizationGroupMutationStatus> AddMemberAsync(
        string organizationId,
        string groupId,
        string actorAccountId,
        string accountIdentifier,
        CancellationToken cancellationToken = default) =>
        MutateMemberAsync(
            organizationId,
            groupId,
            actorAccountId,
            accountIdentifier,
            true,
            cancellationToken);

    public Task<OrganizationGroupMutationStatus> DeleteMemberAsync(
        string organizationId,
        string groupId,
        string actorAccountId,
        string accountId,
        CancellationToken cancellationToken = default) =>
        MutateMemberAsync(
            organizationId,
            groupId,
            actorAccountId,
            accountId,
            false,
            cancellationToken);

    private async Task<OrganizationGroupMutationStatus> MutateMemberAsync(
        string organizationId,
        string groupId,
        string actorAccountId,
        string accountId,
        bool add,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(groupId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (!await LockOrganizationAsync(
                connection,
                transaction,
                organizationId,
                cancellationToken))
        {
            return OrganizationGroupMutationStatus.OrganizationNotFound;
        }

        var actorRole = await GetRoleAsync(
            connection,
            transaction,
            organizationId,
            actorAccountId,
            cancellationToken);
        if (actorRole is null)
        {
            return OrganizationGroupMutationStatus.OrganizationNotFound;
        }

        if (!OrganizationRoles.CanManageGroups(actorRole))
        {
            return OrganizationGroupMutationStatus.Forbidden;
        }

        if (await GetGroupRowAsync(
                connection,
                transaction,
                organizationId,
                groupId,
                cancellationToken) is null)
        {
            return OrganizationGroupMutationStatus.GroupNotFound;
        }

        if (add)
        {
            const string RESOLVE_MEMBER_QUERY = """
                SELECT `m`.`account_id`
                FROM `organization_member` `m`
                INNER JOIN `account` `a`
                    ON `a`.`id` = `m`.`account_id`
                WHERE `m`.`organization_id` = @organizationId
                    AND (`a`.`id` = @accountId OR `a`.`email` = @accountId)
                ORDER BY
                    CASE WHEN `a`.`id` = @accountId THEN 0 ELSE 1 END,
                    `a`.`id`
                LIMIT 1
                """;
            var resolveMemberCommand = new CommandDefinition(
                RESOLVE_MEMBER_QUERY,
                new { organizationId, accountId },
                transaction,
                cancellationToken: cancellationToken);
            var resolvedAccountId = await connection.QuerySingleOrDefaultAsync<string>(
                resolveMemberCommand);
            if (resolvedAccountId is null)
            {
                return OrganizationGroupMutationStatus.MemberNotFound;
            }

            accountId = resolvedAccountId;
        }

        if (await GetRoleAsync(
                connection,
                transaction,
                organizationId,
                accountId,
                cancellationToken) is null)
        {
            return OrganizationGroupMutationStatus.MemberNotFound;
        }

        const string EXISTS_QUERY = """
            SELECT `account_id`
            FROM `organization_group_member`
            WHERE `organization_id` = @organizationId
                AND `group_id` = @groupId
                AND `account_id` = @accountId
            """;
        var command = new CommandDefinition(
            EXISTS_QUERY,
            new { organizationId, groupId, accountId },
            transaction,
            cancellationToken: cancellationToken);
        var exists = await connection.QuerySingleOrDefaultAsync<string>(command) is not null;
        if (add && exists)
        {
            return OrganizationGroupMutationStatus.AlreadyMember;
        }

        if (!add && !exists)
        {
            return OrganizationGroupMutationStatus.MemberNotFound;
        }

        var query = add
            ? """
                INSERT INTO `organization_group_member`
                    (`organization_id`, `group_id`, `account_id`, `created_at`)
                VALUES
                    (@organizationId, @groupId, @accountId, @createdAt)
                """
            : """
                DELETE FROM `organization_group_member`
                WHERE `organization_id` = @organizationId
                    AND `group_id` = @groupId
                    AND `account_id` = @accountId
                """;
        command = new CommandDefinition(
            query,
            new { organizationId, groupId, accountId, createdAt = DateTime.UtcNow },
            transaction,
            cancellationToken: cancellationToken);
        try
        {
            if (await connection.ExecuteAsync(command) != 1)
            {
                throw new InvalidOperationException("Failed to update an organization group member.");
            }
        }
        catch (MySqlException exception) when (add && exception.Number == 1062)
        {
            return OrganizationGroupMutationStatus.AlreadyMember;
        }

        await transaction.CommitAsync(cancellationToken);
        return OrganizationGroupMutationStatus.Succeeded;
    }

    private static async Task<bool> LockOrganizationAsync(
        MySqlConnection connection,
        System.Data.Common.DbTransaction transaction,
        string organizationId,
        CancellationToken cancellationToken)
    {
        const string QUERY = """
            SELECT `id`
            FROM `organization`
            WHERE `id` = @organizationId
            FOR UPDATE
            """;
        var command = new CommandDefinition(
            QUERY,
            new { organizationId },
            transaction,
            cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<string>(command) is not null;
    }

    private static async Task<string?> GetRoleAsync(
        MySqlConnection connection,
        System.Data.Common.DbTransaction? transaction,
        string organizationId,
        string accountId,
        CancellationToken cancellationToken)
    {
        const string QUERY = """
            SELECT `role`
            FROM `organization_member`
            WHERE `organization_id` = @organizationId
                AND `account_id` = @accountId
            """;
        var command = new CommandDefinition(
            QUERY,
            new { organizationId, accountId },
            transaction,
            cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<string>(command);
    }

    private static async Task<OrganizationGroupRow?> GetGroupRowAsync(
        MySqlConnection connection,
        System.Data.Common.DbTransaction? transaction,
        string organizationId,
        string groupId,
        CancellationToken cancellationToken)
    {
        const string QUERY = """
            SELECT
                `g`.`organization_id` AS `OrganizationId`,
                `g`.`id` AS `Id`,
                `g`.`name` AS `Name`,
                `g`.`created_at` AS `CreatedAt`,
                COUNT(`gm`.`account_id`) AS `MemberCount`
            FROM `organization_group` `g`
            LEFT JOIN `organization_group_member` `gm`
                ON `gm`.`organization_id` = `g`.`organization_id`
                AND `gm`.`group_id` = `g`.`id`
            WHERE `g`.`organization_id` = @organizationId
                AND `g`.`id` = @groupId
            GROUP BY
                `g`.`organization_id`,
                `g`.`id`,
                `g`.`name`,
                `g`.`created_at`
            """;
        var command = new CommandDefinition(
            QUERY,
            new { organizationId, groupId },
            transaction,
            cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<OrganizationGroupRow>(command);
    }

    private sealed record OrganizationGroupRow
    {
        public required string OrganizationId { get; init; }

        public required string Id { get; init; }

        public required string Name { get; init; }

        public DateTime CreatedAt { get; init; }

        public long MemberCount { get; init; }

        public OrganizationGroup ToGroup() => new()
        {
            OrganizationId = OrganizationId,
            Id = Id,
            Name = Name,
            CreatedAt = CreatedAt
        };
    }
}
