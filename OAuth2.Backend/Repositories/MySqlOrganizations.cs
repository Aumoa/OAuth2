using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Data;
using OAuth2.Options;

namespace OAuth2.Repositories;

internal sealed class MySqlOrganizations(IOptions<MySqlOptions> mysqlOptions) : IOrganizations
{
    private const string OwnerRole = "owner";

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
            const string ORGANIZATION_QUERY = """
                INSERT INTO `organization` (`id`, `name`, `created_by`, `created_at`)
                VALUES (@Id, @Name, @accountId, @CreatedAt)
                """;
            var command = new CommandDefinition(
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
                    role = OwnerRole,
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
                Role = OwnerRole
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
