using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Data;
using OAuth2.OpenId;
using OAuth2.Options;

namespace OAuth2.Repositories;

internal sealed class MySqlApplications(IOptions<MySqlOptions> mysqlOptions) : IApplications
{
    private const string RedirectUriClaimName = "redirect_uri";
    private const string ScopeClaimName = "scope";

    private sealed record ApplicationClaim(string ClientId, string Name, string Value);

    public async Task<OAuthApplication?> AddApplicationAsync(
        string id,
        string ownerId,
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var application = new OAuthApplication
        {
            Id = id,
            OwnerId = ownerId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string QUERY = "INSERT INTO `client` (`id`, `owner_id`, `name`, `created_at`) VALUES (@Id, @OwnerId, @Name, @CreatedAt)";
        var command = new CommandDefinition(
            QUERY,
            application,
            transaction,
            cancellationToken: cancellationToken);

        try
        {
            if (await connection.ExecuteAsync(command) != 1)
            {
                return null;
            }

            const string CLAIM_QUERY = "INSERT INTO `client_claim` (`client_id`, `name`, `value`) VALUES (@ClientId, @Name, @Value)";
            var defaultScopes = OidcScopePolicy.ClaimScopes
                .Select(scope => new ApplicationClaim(id, ScopeClaimName, scope))
                .ToArray();
            command = new CommandDefinition(
                CLAIM_QUERY,
                defaultScopes,
                transaction,
                cancellationToken: cancellationToken);
            if (await connection.ExecuteAsync(command) != defaultScopes.Length)
            {
                throw new InvalidOperationException("Failed to add the default application scopes.");
            }

            await transaction.CommitAsync(cancellationToken);
            return application;
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            return null;
        }
    }

    public async Task<bool> DeleteApplicationAsync(
        string id,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string LOCK_QUERY = "SELECT `id` FROM `client` WHERE `id` = @id AND `owner_id` = @ownerId AND `removed_at` IS NULL FOR UPDATE";
        var command = new CommandDefinition(
            LOCK_QUERY,
            new { id, ownerId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.QuerySingleOrDefaultAsync<string>(command) is null)
        {
            return false;
        }

        var removedAt = DateTime.UtcNow;
        const string REVOKE_API_KEYS_QUERY = "UPDATE `client_api_key` SET `removed_at` = @removedAt WHERE `removed_at` IS NULL AND (`client_id` = @id OR `allowed_client_id` = @id)";
        command = new CommandDefinition(
            REVOKE_API_KEYS_QUERY,
            new { id, removedAt },
            transaction,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);

        foreach (var query in new[]
        {
            "DELETE FROM `client_claim` WHERE `client_id` = @id",
            "DELETE FROM `client_user_group` WHERE `client_id` = @id",
            "DELETE FROM `oauth_grant` WHERE `client_id` = @id"
        })
        {
            command = new CommandDefinition(
                query,
                new { id },
                transaction,
                cancellationToken: cancellationToken);
            await connection.ExecuteAsync(command);
        }

        const string DELETE_QUERY = "DELETE FROM `client` WHERE `id` = @id AND `owner_id` = @ownerId";
        command = new CommandDefinition(
            DELETE_QUERY,
            new { id, ownerId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<OAuthApplicationConfiguration?> GetOwnedApplicationAsync(
        string id,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);

        const string APPLICATION_QUERY = "SELECT `id`, `owner_id` AS `OwnerId`, `name`, `created_at` AS `CreatedAt` FROM `client` WHERE `id` = @id AND `owner_id` = @ownerId AND `removed_at` IS NULL";
        var command = new CommandDefinition(
            APPLICATION_QUERY,
            new { id, ownerId },
            cancellationToken: cancellationToken);
        var application = await connection.QuerySingleOrDefaultAsync<OAuthApplication>(command);
        if (application is null)
        {
            return null;
        }

        const string CLAIM_QUERY = "SELECT `value` FROM `client_claim` WHERE `client_id` = @id AND `name` = @name AND `removed_at` IS NULL ORDER BY `id`";
        command = new CommandDefinition(
            CLAIM_QUERY,
            new { id, name = RedirectUriClaimName },
            cancellationToken: cancellationToken);
        var redirectUris = (await connection.QueryAsync<string>(command)).ToArray();

        command = new CommandDefinition(
            CLAIM_QUERY,
            new { id, name = ScopeClaimName },
            cancellationToken: cancellationToken);
        var allowedScopes = (await connection.QueryAsync<string>(command)).ToArray();

        return new OAuthApplicationConfiguration
        {
            Application = application,
            RedirectUris = redirectUris,
            AllowedScopes = allowedScopes
        };
    }

    public async Task<IReadOnlyList<OAuthApplication>> GetOwnedApplicationsAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);

        const string QUERY = """
            SELECT
                `id`,
                `owner_id` AS `OwnerId`,
                `name`,
                `created_at` AS `CreatedAt`
            FROM `client`
            WHERE `owner_id` = @ownerId
                AND `removed_at` IS NULL
            ORDER BY `created_at` DESC, `id`
            """;
        var command = new CommandDefinition(
            QUERY,
            new { ownerId },
            cancellationToken: cancellationToken);
        var applications = await connection.QueryAsync<OAuthApplication>(command);
        return applications.ToArray();
    }

    public async Task<bool> UpdateApplicationAsync(
        string id,
        string ownerId,
        IReadOnlyCollection<string> redirectUris,
        IReadOnlyCollection<string> allowedScopes,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(redirectUris);
        ArgumentNullException.ThrowIfNull(allowedScopes);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string LOCK_QUERY = "SELECT `id` FROM `client` WHERE `id` = @id AND `owner_id` = @ownerId AND `removed_at` IS NULL FOR UPDATE";
        var command = new CommandDefinition(
            LOCK_QUERY,
            new { id, ownerId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.QuerySingleOrDefaultAsync<string>(command) is null)
        {
            return false;
        }

        const string REMOVE_CLAIMS_QUERY = "UPDATE `client_claim` SET `removed_at` = @removedAt WHERE `client_id` = @id AND `name` IN @names AND `removed_at` IS NULL";
        command = new CommandDefinition(
            REMOVE_CLAIMS_QUERY,
            new
            {
                id,
                names = new[] { RedirectUriClaimName, ScopeClaimName },
                removedAt = DateTime.UtcNow
            },
            transaction,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);

        var claims = redirectUris
            .Select(value => new ApplicationClaim(id, RedirectUriClaimName, value))
            .Concat(allowedScopes.Select(value => new ApplicationClaim(id, ScopeClaimName, value)))
            .ToArray();
        if (claims.Length > 0)
        {
            const string ADD_CLAIMS_QUERY = "INSERT INTO `client_claim` (`client_id`, `name`, `value`) VALUES (@ClientId, @Name, @Value)";
            command = new CommandDefinition(
                ADD_CLAIMS_QUERY,
                claims,
                transaction,
                cancellationToken: cancellationToken);
            if (await connection.ExecuteAsync(command) != claims.Length)
            {
                throw new InvalidOperationException("Failed to update the application configuration.");
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
