using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Data;
using OAuth2.Options;

namespace OAuth2.Repositories;

internal sealed class MySqlApplicationRoles(IOptions<MySqlOptions> mysqlOptions)
    : IApplicationRoles
{
    public async Task<IReadOnlyList<(OAuthApplicationRole Role, long MemberCount)>?> GetRolesAsync(
        string clientId,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        if (!await ApplicationExistsAsync(
                connection,
                null,
                clientId,
                ownerId,
                false,
                cancellationToken))
        {
            return null;
        }

        const string QUERY = """
            SELECT
                `r`.`client_id` AS `ClientId`,
                `r`.`id` AS `Id`,
                `r`.`name` AS `Name`,
                `r`.`created_at` AS `CreatedAt`,
                COUNT(`a`.`account_id`) AS `MemberCount`
            FROM `client_role` `r`
            LEFT JOIN `client_role_assignment` `a`
                ON `a`.`client_id` = `r`.`client_id`
                AND `a`.`role_id` = `r`.`id`
            WHERE `r`.`client_id` = @clientId
            GROUP BY `r`.`client_id`, `r`.`id`, `r`.`name`, `r`.`created_at`
            ORDER BY `r`.`name`, `r`.`id`
            """;
        var command = new CommandDefinition(
            QUERY,
            new { clientId },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<ApplicationRoleRow>(command);
        return rows.Select(static row => (row.ToRole(), row.MemberCount)).ToArray();
    }

    public async Task<(ApplicationRoleMutationStatus Status, OAuthApplicationRole? Role)> AddRoleAsync(
        string clientId,
        string ownerId,
        string roleId,
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (!await ApplicationExistsAsync(
                connection,
                transaction,
                clientId,
                ownerId,
                true,
                cancellationToken))
        {
            return (ApplicationRoleMutationStatus.ApplicationNotFound, null);
        }

        const string COUNT_QUERY = "SELECT COUNT(*) FROM `client_role` WHERE `client_id` = @clientId";
        var command = new CommandDefinition(
            COUNT_QUERY,
            new { clientId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.QuerySingleAsync<int>(command) >= IApplicationRoles.MaxRoles)
        {
            return (ApplicationRoleMutationStatus.RoleLimitReached, null);
        }

        var role = new OAuthApplicationRole
        {
            ClientId = clientId,
            Id = roleId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        const string INSERT_QUERY = """
            INSERT INTO `client_role` (`client_id`, `id`, `name`, `created_at`)
            VALUES (@ClientId, @Id, @Name, @CreatedAt)
            """;
        command = new CommandDefinition(
            INSERT_QUERY,
            role,
            transaction,
            cancellationToken: cancellationToken);
        try
        {
            if (await connection.ExecuteAsync(command) != 1)
            {
                throw new InvalidOperationException("Failed to create an application role.");
            }
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            return (ApplicationRoleMutationStatus.RoleExists, null);
        }

        await transaction.CommitAsync(cancellationToken);
        return (ApplicationRoleMutationStatus.Succeeded, role);
    }

    public async Task<ApplicationRoleMutationStatus> DeleteRoleAsync(
        string clientId,
        string ownerId,
        string roleId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (!await ApplicationExistsAsync(
                connection,
                transaction,
                clientId,
                ownerId,
                true,
                cancellationToken))
        {
            return ApplicationRoleMutationStatus.ApplicationNotFound;
        }

        const string QUERY = "DELETE FROM `client_role` WHERE `client_id` = @clientId AND `id` = @roleId";
        var command = new CommandDefinition(
            QUERY,
            new { clientId, roleId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            return ApplicationRoleMutationStatus.RoleNotFound;
        }

        await transaction.CommitAsync(cancellationToken);
        return ApplicationRoleMutationStatus.Succeeded;
    }

    public async Task<(IReadOnlyList<OAuthApplicationRoleMember> Items, long TotalCount)?> GetMembersAsync(
        string clientId,
        string ownerId,
        string roleId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(page);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        if (!await RoleExistsAsync(
                connection,
                null,
                clientId,
                ownerId,
                roleId,
                cancellationToken))
        {
            return null;
        }

        const string COUNT_QUERY = """
            SELECT COUNT(*)
            FROM `client_role_assignment`
            WHERE `client_id` = @clientId AND `role_id` = @roleId
            """;
        var command = new CommandDefinition(
            COUNT_QUERY,
            new { clientId, roleId },
            cancellationToken: cancellationToken);
        var totalCount = await connection.QuerySingleAsync<long>(command);

        const string PAGE_QUERY = """
            SELECT
                `account`.`id` AS `AccountId`,
                `account`.`name` AS `Name`,
                `account`.`email` AS `Email`,
                `assignment`.`created_at` AS `AssignedAt`
            FROM `client_role_assignment` `assignment`
            INNER JOIN `account`
                ON `account`.`id` = `assignment`.`account_id`
            WHERE `assignment`.`client_id` = @clientId
                AND `assignment`.`role_id` = @roleId
            ORDER BY `account`.`name`, `account`.`id`
            LIMIT @pageSize OFFSET @offset
            """;
        var offset = checked((long)(page - 1) * pageSize);
        command = new CommandDefinition(
            PAGE_QUERY,
            new { clientId, roleId, pageSize, offset },
            cancellationToken: cancellationToken);
        var members = await connection.QueryAsync<OAuthApplicationRoleMember>(command);
        return (members.ToArray(), totalCount);
    }

    public async Task<ApplicationRoleMutationStatus> AddMemberAsync(
        string clientId,
        string ownerId,
        string roleId,
        string accountIdentifier,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountIdentifier);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (!await ApplicationExistsAsync(
                connection,
                transaction,
                clientId,
                ownerId,
                true,
                cancellationToken))
        {
            return ApplicationRoleMutationStatus.ApplicationNotFound;
        }

        if (!await RoleExistsAsync(
                connection,
                transaction,
                clientId,
                ownerId,
                roleId,
                cancellationToken))
        {
            return ApplicationRoleMutationStatus.RoleNotFound;
        }

        const string ACCOUNT_QUERY = """
            SELECT `id`
            FROM `account`
            WHERE (`id` = @accountIdentifier OR `email` = @accountIdentifier)
            LIMIT 1
            """;
        var command = new CommandDefinition(
            ACCOUNT_QUERY,
            new { accountIdentifier },
            transaction,
            cancellationToken: cancellationToken);
        var accountId = await connection.QuerySingleOrDefaultAsync<string>(command);
        if (accountId is null)
        {
            return ApplicationRoleMutationStatus.AccountNotFound;
        }

        const string INSERT_QUERY = """
            INSERT INTO `client_role_assignment`
                (`client_id`, `role_id`, `account_id`, `created_at`)
            VALUES (@clientId, @roleId, @accountId, @createdAt)
            """;
        command = new CommandDefinition(
            INSERT_QUERY,
            new { clientId, roleId, accountId, createdAt = DateTime.UtcNow },
            transaction,
            cancellationToken: cancellationToken);
        try
        {
            if (await connection.ExecuteAsync(command) != 1)
            {
                throw new InvalidOperationException("Failed to assign an application role.");
            }
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            return ApplicationRoleMutationStatus.AlreadyAssigned;
        }

        await transaction.CommitAsync(cancellationToken);
        return ApplicationRoleMutationStatus.Succeeded;
    }

    public async Task<ApplicationRoleMutationStatus> DeleteMemberAsync(
        string clientId,
        string ownerId,
        string roleId,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(roleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        if (!await ApplicationExistsAsync(
                connection,
                transaction,
                clientId,
                ownerId,
                true,
                cancellationToken))
        {
            return ApplicationRoleMutationStatus.ApplicationNotFound;
        }

        if (!await RoleExistsAsync(
                connection,
                transaction,
                clientId,
                ownerId,
                roleId,
                cancellationToken))
        {
            return ApplicationRoleMutationStatus.RoleNotFound;
        }

        const string QUERY = """
            DELETE FROM `client_role_assignment`
            WHERE `client_id` = @clientId
                AND `role_id` = @roleId
                AND `account_id` = @accountId
            """;
        var command = new CommandDefinition(
            QUERY,
            new { clientId, roleId, accountId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            return ApplicationRoleMutationStatus.AccountNotFound;
        }

        await transaction.CommitAsync(cancellationToken);
        return ApplicationRoleMutationStatus.Succeeded;
    }

    public async Task<IReadOnlyList<string>> GetAssignedRoleIdsAsync(
        string clientId,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        const string QUERY = """
            SELECT `role_id`
            FROM `client_role_assignment`
            WHERE `client_id` = @clientId AND `account_id` = @accountId
            ORDER BY `role_id`
            """;
        var command = new CommandDefinition(
            QUERY,
            new { clientId, accountId },
            cancellationToken: cancellationToken);
        return (await connection.QueryAsync<string>(command)).ToArray();
    }

    private static async Task<bool> ApplicationExistsAsync(
        MySqlConnection connection,
        System.Data.Common.DbTransaction? transaction,
        string clientId,
        string ownerId,
        bool lockForUpdate,
        CancellationToken cancellationToken)
    {
        var query = """
            SELECT `id`
            FROM `client`
            WHERE `id` = @clientId
                AND `owner_id` = @ownerId
                AND `removed_at` IS NULL
            """ + (lockForUpdate ? " FOR UPDATE" : string.Empty);
        var command = new CommandDefinition(
            query,
            new { clientId, ownerId },
            transaction,
            cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<string>(command) is not null;
    }

    private static async Task<bool> RoleExistsAsync(
        MySqlConnection connection,
        System.Data.Common.DbTransaction? transaction,
        string clientId,
        string ownerId,
        string roleId,
        CancellationToken cancellationToken)
    {
        const string QUERY = """
            SELECT COUNT(*)
            FROM `client_role` `role`
            INNER JOIN `client`
                ON `client`.`id` = `role`.`client_id`
                AND `client`.`owner_id` = @ownerId
                AND `client`.`removed_at` IS NULL
            WHERE `role`.`client_id` = @clientId AND `role`.`id` = @roleId
            """;
        var command = new CommandDefinition(
            QUERY,
            new { clientId, ownerId, roleId },
            transaction,
            cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<int>(command) == 1;
    }

    private sealed record ApplicationRoleRow
    {
        public required string ClientId { get; init; }

        public required string Id { get; init; }

        public required string Name { get; init; }

        public DateTime CreatedAt { get; init; }

        public long MemberCount { get; init; }

        public OAuthApplicationRole ToRole() => new()
        {
            ClientId = ClientId,
            Id = Id,
            Name = Name,
            CreatedAt = CreatedAt
        };
    }
}
