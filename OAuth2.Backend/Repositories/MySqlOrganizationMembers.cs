using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Data;
using OAuth2.Options;

namespace OAuth2.Repositories;

internal sealed class MySqlOrganizationMembers(IOptions<MySqlOptions> mysqlOptions)
    : IOrganizationMembers
{
    public async Task<(IReadOnlyList<OrganizationMember> Items, long TotalCount)?> GetPageAsync(
        string organizationId,
        string actorAccountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorAccountId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string MEMBERSHIP_QUERY = """
            SELECT `m`.`role`
            FROM `organization_member` `m`
            INNER JOIN `organization` `o`
                ON `o`.`id` = `m`.`organization_id`
            WHERE `m`.`organization_id` = @organizationId
                AND `m`.`account_id` = @actorAccountId
            """;
        var command = new CommandDefinition(
            MEMBERSHIP_QUERY,
            new { organizationId, actorAccountId },
            cancellationToken: cancellationToken);
        if (await connection.QuerySingleOrDefaultAsync<string>(command) is null)
        {
            return null;
        }

        const string COUNT_QUERY = """
            SELECT COUNT(*)
            FROM `organization_member` `m`
            INNER JOIN `account` `a`
                ON `a`.`id` = `m`.`account_id`
            WHERE `m`.`organization_id` = @organizationId
            """;
        command = new CommandDefinition(
            COUNT_QUERY,
            new { organizationId },
            cancellationToken: cancellationToken);
        var totalCount = await connection.QuerySingleAsync<long>(command);

        const string PAGE_QUERY = """
            SELECT
                `a`.`id` AS `AccountId`,
                `a`.`name` AS `Name`,
                `m`.`role` AS `Role`,
                `m`.`created_at` AS `JoinedAt`
            FROM `organization_member` `m`
            INNER JOIN `account` `a`
                ON `a`.`id` = `m`.`account_id`
            WHERE `m`.`organization_id` = @organizationId
            ORDER BY
                CASE `m`.`role`
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
            new { organizationId, pageSize, offset },
            cancellationToken: cancellationToken);
        var members = await connection.QueryAsync<OrganizationMember>(command);
        return (members.ToArray(), totalCount);
    }

    public async Task<IReadOnlyList<OrganizationClaimValue>> GetClaimsAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        const string QUERY = """
            SELECT
                `o`.`id` AS `Id`,
                `o`.`name` AS `Name`,
                `m`.`role` AS `Role`
            FROM `organization_member` `m`
            INNER JOIN `organization` `o`
                ON `o`.`id` = `m`.`organization_id`
            WHERE `m`.`account_id` = @accountId
            ORDER BY `o`.`id`
            """;
        var command = new CommandDefinition(
            QUERY,
            new { accountId },
            cancellationToken: cancellationToken);
        var claims = await connection.QueryAsync<OrganizationClaimValue>(command);
        return claims.ToArray();
    }

    public async Task<OrganizationMemberMutationStatus> AddAsync(
        string organizationId,
        string actorAccountId,
        string accountId,
        string role,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        if (!OrganizationRoles.IsAssignable(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (!await LockOrganizationAsync(connection, transaction, organizationId, cancellationToken))
        {
            return OrganizationMemberMutationStatus.OrganizationNotFound;
        }

        var actorRole = await GetRoleAsync(
            connection,
            transaction,
            organizationId,
            actorAccountId,
            cancellationToken);
        if (actorRole is null)
        {
            return OrganizationMemberMutationStatus.OrganizationNotFound;
        }

        if (!OrganizationRoles.CanAdd(actorRole, role))
        {
            return OrganizationMemberMutationStatus.Forbidden;
        }

        const string ACCOUNT_QUERY = """
            SELECT `id`
            FROM `account`
            WHERE `id` = @accountId
                AND `verify_code` IS NULL
            """;
        var command = new CommandDefinition(
            ACCOUNT_QUERY,
            new { accountId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.QuerySingleOrDefaultAsync<string>(command) is null)
        {
            return OrganizationMemberMutationStatus.AccountNotFound;
        }

        if (await GetRoleAsync(
                connection,
                transaction,
                organizationId,
                accountId,
                cancellationToken) is not null)
        {
            return OrganizationMemberMutationStatus.AlreadyMember;
        }

        const string INSERT_QUERY = """
            INSERT INTO `organization_member`
                (`organization_id`, `account_id`, `role`, `created_at`)
            VALUES
                (@organizationId, @accountId, @role, @createdAt)
            """;
        command = new CommandDefinition(
            INSERT_QUERY,
            new { organizationId, accountId, role, createdAt = DateTime.UtcNow },
            transaction,
            cancellationToken: cancellationToken);
        try
        {
            if (await connection.ExecuteAsync(command) != 1)
            {
                throw new InvalidOperationException("Failed to add an organization member.");
            }
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            return OrganizationMemberMutationStatus.AlreadyMember;
        }

        await transaction.CommitAsync(cancellationToken);
        return OrganizationMemberMutationStatus.Succeeded;
    }

    public async Task<OrganizationMemberMutationStatus> UpdateRoleAsync(
        string organizationId,
        string actorAccountId,
        string accountId,
        string role,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        if (!OrganizationRoles.IsAssignable(role))
        {
            throw new ArgumentOutOfRangeException(nameof(role));
        }

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var context = await GetMutationContextAsync(
            connection,
            transaction,
            organizationId,
            actorAccountId,
            accountId,
            cancellationToken);
        if (context.Status != OrganizationMemberMutationStatus.Succeeded)
        {
            return context.Status;
        }

        if (!OrganizationRoles.CanManage(context.ActorRole, context.TargetRole)
            || !OrganizationRoles.CanAssign(context.ActorRole, role))
        {
            return OrganizationMemberMutationStatus.Forbidden;
        }

        if (!string.Equals(context.TargetRole, role, StringComparison.Ordinal))
        {
            const string QUERY = """
                UPDATE `organization_member`
                SET `role` = @role
                WHERE `organization_id` = @organizationId
                    AND `account_id` = @accountId
                """;
            var command = new CommandDefinition(
                QUERY,
                new { organizationId, accountId, role },
                transaction,
                cancellationToken: cancellationToken);
            if (await connection.ExecuteAsync(command) != 1)
            {
                throw new InvalidOperationException("Failed to update an organization member role.");
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return OrganizationMemberMutationStatus.Succeeded;
    }

    public async Task<OrganizationMemberMutationStatus> DeleteAsync(
        string organizationId,
        string actorAccountId,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var context = await GetMutationContextAsync(
            connection,
            transaction,
            organizationId,
            actorAccountId,
            accountId,
            cancellationToken);
        if (context.Status != OrganizationMemberMutationStatus.Succeeded)
        {
            return context.Status;
        }

        if (!OrganizationRoles.CanManage(context.ActorRole, context.TargetRole))
        {
            return OrganizationMemberMutationStatus.Forbidden;
        }

        const string QUERY = """
            DELETE FROM `organization_member`
            WHERE `organization_id` = @organizationId
                AND `account_id` = @accountId
            """;
        var command = new CommandDefinition(
            QUERY,
            new { organizationId, accountId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            throw new InvalidOperationException("Failed to delete an organization member.");
        }

        await transaction.CommitAsync(cancellationToken);
        return OrganizationMemberMutationStatus.Succeeded;
    }

    public async Task<OrganizationMemberMutationStatus> TransferOwnershipAsync(
        string organizationId,
        string actorAccountId,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(actorAccountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var context = await GetMutationContextAsync(
            connection,
            transaction,
            organizationId,
            actorAccountId,
            accountId,
            cancellationToken);
        if (context.Status != OrganizationMemberMutationStatus.Succeeded)
        {
            return context.Status;
        }

        if (!string.Equals(context.ActorRole, OrganizationRoles.Owner, StringComparison.Ordinal)
            || !OrganizationRoles.CanManage(context.ActorRole, context.TargetRole))
        {
            return OrganizationMemberMutationStatus.Forbidden;
        }

        const string DEMOTE_QUERY = """
            UPDATE `organization_member`
            SET `role` = @role
            WHERE `organization_id` = @organizationId
                AND `account_id` = @actorAccountId
                AND `role` = @ownerRole
            """;
        var command = new CommandDefinition(
            DEMOTE_QUERY,
            new
            {
                organizationId,
                actorAccountId,
                role = OrganizationRoles.Admin,
                ownerRole = OrganizationRoles.Owner
            },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            throw new InvalidOperationException("Failed to demote the previous organization owner.");
        }

        const string PROMOTE_QUERY = """
            UPDATE `organization_member`
            SET `role` = @role
            WHERE `organization_id` = @organizationId
                AND `account_id` = @accountId
            """;
        command = new CommandDefinition(
            PROMOTE_QUERY,
            new { organizationId, accountId, role = OrganizationRoles.Owner },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            throw new InvalidOperationException("Failed to promote the new organization owner.");
        }

        await transaction.CommitAsync(cancellationToken);
        return OrganizationMemberMutationStatus.Succeeded;
    }

    private static async Task<MutationContext> GetMutationContextAsync(
        MySqlConnection connection,
        System.Data.Common.DbTransaction transaction,
        string organizationId,
        string actorAccountId,
        string accountId,
        CancellationToken cancellationToken)
    {
        if (!await LockOrganizationAsync(connection, transaction, organizationId, cancellationToken))
        {
            return new MutationContext(OrganizationMemberMutationStatus.OrganizationNotFound);
        }

        var actorRole = await GetRoleAsync(
            connection,
            transaction,
            organizationId,
            actorAccountId,
            cancellationToken);
        if (actorRole is null)
        {
            return new MutationContext(OrganizationMemberMutationStatus.OrganizationNotFound);
        }

        var targetRole = await GetRoleAsync(
            connection,
            transaction,
            organizationId,
            accountId,
            cancellationToken);
        return targetRole is null
            ? new MutationContext(OrganizationMemberMutationStatus.MemberNotFound)
            : new MutationContext(
                OrganizationMemberMutationStatus.Succeeded,
                actorRole,
                targetRole);
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
        System.Data.Common.DbTransaction transaction,
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

    private sealed record MutationContext(
        OrganizationMemberMutationStatus Status,
        string? ActorRole = null,
        string? TargetRole = null);
}
