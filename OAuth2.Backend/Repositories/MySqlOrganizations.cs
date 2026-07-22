using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Data;
using OAuth2.Options;

namespace OAuth2.Repositories;

internal sealed class MySqlOrganizations(IOptions<MySqlOptions> mysqlOptions) : IOrganizations
{
    public async Task<OrganizationMembership?> AddOrganizationAsync(
        string id,
        string name,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var organization = new OAuthOrganization
        {
            Id = id,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var applicationOwnerId = ApplicationOwnerIds.CreateForOrganization(id);
            const string OWNER_CONFLICT_QUERY = """
                SELECT `id`
                FROM `account`
                WHERE `id` = @applicationOwnerId
                """;
            var command = new CommandDefinition(
                OWNER_CONFLICT_QUERY,
                new { applicationOwnerId },
                transaction,
                cancellationToken: cancellationToken);
            if (await connection.QuerySingleOrDefaultAsync<string>(command) is not null)
            {
                return null;
            }

            const string ORGANIZATION_QUERY = """
                INSERT INTO `organization` (`id`, `name`, `created_by`, `created_at`)
                VALUES (@Id, @Name, @accountId, @CreatedAt)
                """;
            command = new CommandDefinition(
                ORGANIZATION_QUERY,
                new { organization.Id, organization.Name, accountId, organization.CreatedAt },
                transaction,
                cancellationToken: cancellationToken);
            if (await connection.ExecuteAsync(command) != 1)
            {
                return null;
            }

            const string MEMBERSHIP_QUERY = """
                INSERT INTO `organization_member` (`organization_id`, `account_id`, `role`, `created_at`)
                VALUES (@organizationId, @accountId, @role, @createdAt)
                """;
            command = new CommandDefinition(
                MEMBERSHIP_QUERY,
                new
                {
                    organizationId = organization.Id,
                    accountId,
                    role = OrganizationRoles.Owner,
                    createdAt = organization.CreatedAt
                },
                transaction,
                cancellationToken: cancellationToken);
            if (await connection.ExecuteAsync(command) != 1)
            {
                throw new InvalidOperationException("Failed to create the organization owner membership.");
            }

            await transaction.CommitAsync(cancellationToken);
            return new OrganizationMembership
            {
                Organization = organization,
                Role = OrganizationRoles.Owner
            };
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            return null;
        }
    }

    public async Task<OrganizationMembership?> GetOrganizationAsync(
        string id,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            OrganizationMembershipQuery + " AND `o`.`id` = @id",
            new { id, accountId },
            cancellationToken: cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<OrganizationMembershipRow>(command);
        return row is null ? null : ToMembership(row);
    }

    public async Task<OrganizationMutationStatus> DeleteOrganizationAsync(
        string id,
        string accountId,
        string confirmationName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(confirmationName);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);

        const string ORGANIZATION_QUERY = """
            SELECT `name`
            FROM `organization`
            WHERE `id` = @id
            FOR UPDATE
            """;
        var command = new CommandDefinition(
            ORGANIZATION_QUERY,
            new { id },
            transaction,
            cancellationToken: cancellationToken);
        var organizationName = await connection.QuerySingleOrDefaultAsync<string>(command);
        if (organizationName is null)
        {
            return OrganizationMutationStatus.OrganizationNotFound;
        }

        const string ROLE_QUERY = """
            SELECT `role`
            FROM `organization_member`
            WHERE `organization_id` = @id
                AND `account_id` = @accountId
            """;
        command = new CommandDefinition(
            ROLE_QUERY,
            new { id, accountId },
            transaction,
            cancellationToken: cancellationToken);
        var actorRole = await connection.QuerySingleOrDefaultAsync<string>(command);
        if (actorRole is null)
        {
            return OrganizationMutationStatus.OrganizationNotFound;
        }

        if (!string.Equals(actorRole, OrganizationRoles.Owner, StringComparison.Ordinal))
        {
            return OrganizationMutationStatus.Forbidden;
        }

        if (!string.Equals(organizationName, confirmationName, StringComparison.Ordinal))
        {
            return OrganizationMutationStatus.ConfirmationMismatch;
        }

        var applicationOwnerId = ApplicationOwnerIds.CreateForOrganization(id);
        const string OWNER_CONFLICT_QUERY = """
            SELECT `id`
            FROM `account`
            WHERE `id` = @applicationOwnerId
            """;
        command = new CommandDefinition(
            OWNER_CONFLICT_QUERY,
            new { applicationOwnerId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.QuerySingleOrDefaultAsync<string>(command) is not null)
        {
            return OrganizationMutationStatus.ApplicationOwnerConflict;
        }

        const string CLIENT_QUERY = """
            SELECT `id`
            FROM `client`
            WHERE `owner_id` = @applicationOwnerId
            FOR UPDATE
            """;
        command = new CommandDefinition(
            CLIENT_QUERY,
            new { applicationOwnerId },
            transaction,
            cancellationToken: cancellationToken);
        var clientIds = (await connection.QueryAsync<string>(command)).ToArray();
        if (clientIds.Length > 0)
        {
            var removedAt = DateTime.UtcNow;
            const string REVOKE_API_KEYS_QUERY = """
                UPDATE `client_api_key`
                SET `removed_at` = @removedAt
                WHERE `removed_at` IS NULL
                    AND (`client_id` IN @clientIds OR `allowed_client_id` IN @clientIds)
                """;
            command = new CommandDefinition(
                REVOKE_API_KEYS_QUERY,
                new { clientIds, removedAt },
                transaction,
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);

            foreach (var query in new[]
            {
                "DELETE FROM `client_claim` WHERE `client_id` IN @clientIds",
                "DELETE FROM `client_role_assignment` WHERE `client_id` IN @clientIds",
                "DELETE FROM `client_role` WHERE `client_id` IN @clientIds",
                "DELETE FROM `oauth_grant` WHERE `client_id` IN @clientIds",
                "DELETE FROM `oauth_refresh_token` WHERE `client_id` IN @clientIds",
                "DELETE FROM `client_secret` WHERE `client_id` IN @clientIds"
            })
            {
                command = new CommandDefinition(
                    query,
                    new { clientIds },
                    transaction,
                    cancellationToken: cancellationToken);
                await connection.ExecuteAsync(command);
            }

            const string DELETE_CLIENTS_QUERY = """
                DELETE FROM `client`
                WHERE `owner_id` = @applicationOwnerId
                """;
            command = new CommandDefinition(
                DELETE_CLIENTS_QUERY,
                new { applicationOwnerId },
                transaction,
                cancellationToken: cancellationToken);
            if (await connection.ExecuteAsync(command) != clientIds.Length)
            {
                throw new InvalidOperationException("Failed to delete organization applications.");
            }
        }

        const string DELETE_GROUP_MEMBERS_QUERY = """
            DELETE FROM `organization_group_member`
            WHERE `organization_id` = @id
            """;
        command = new CommandDefinition(
            DELETE_GROUP_MEMBERS_QUERY,
            new { id },
            transaction,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);

        const string DELETE_GROUPS_QUERY = """
            DELETE FROM `organization_group`
            WHERE `organization_id` = @id
            """;
        command = new CommandDefinition(
            DELETE_GROUPS_QUERY,
            new { id },
            transaction,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);

        const string DELETE_MEMBERS_QUERY = """
            DELETE FROM `organization_member`
            WHERE `organization_id` = @id
            """;
        command = new CommandDefinition(
            DELETE_MEMBERS_QUERY,
            new { id },
            transaction,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);

        const string DELETE_ORGANIZATION_QUERY = """
            DELETE FROM `organization`
            WHERE `id` = @id
            """;
        command = new CommandDefinition(
            DELETE_ORGANIZATION_QUERY,
            new { id },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            throw new InvalidOperationException("Failed to delete the organization.");
        }

        await transaction.CommitAsync(cancellationToken);
        return OrganizationMutationStatus.Succeeded;
    }

    public async Task<IReadOnlyList<OrganizationMembership>> GetOrganizationsAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var command = new CommandDefinition(
            OrganizationMembershipQuery + " ORDER BY `o`.`name`, `o`.`id`",
            new { accountId },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<OrganizationMembershipRow>(command);
        return rows.Select(ToMembership).ToArray();
    }

    private const string OrganizationMembershipQuery = """
        SELECT
            `o`.`id` AS `Id`,
            `o`.`name` AS `Name`,
            `o`.`created_at` AS `CreatedAt`,
            `m`.`role` AS `Role`
        FROM `organization` `o`
        INNER JOIN `organization_member` `m`
            ON `m`.`organization_id` = `o`.`id`
        WHERE `m`.`account_id` = @accountId
        """;

    private static OrganizationMembership ToMembership(OrganizationMembershipRow row) => new()
    {
        Organization = new OAuthOrganization
        {
            Id = row.Id,
            Name = row.Name,
            CreatedAt = row.CreatedAt
        },
        Role = row.Role
    };

    private sealed record OrganizationMembershipRow
    {
        public required string Id { get; init; }

        public required string Name { get; init; }

        public DateTime CreatedAt { get; init; }

        public required string Role { get; init; }
    }
}
