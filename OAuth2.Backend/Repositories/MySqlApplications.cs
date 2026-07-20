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

    private const string GroupClaimFormatClaimName = "groups_claim_format";

    private const string GroupClaimSelectorClaimName = "groups_claim_selector";

    private sealed record ApplicationClaim(string ClientId, string Name, string Value);

    public async Task<OAuthApplication?> AddApplicationAsync(
        string id,
        string ownerId,
        string name,
        string applicationType,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!OAuthApplicationTypes.IsSupported(applicationType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(applicationType),
                applicationType,
                "Unsupported OAuth application type.");
        }

        var application = new OAuthApplication
        {
            Id = id,
            OwnerId = ownerId,
            Name = name,
            ApplicationType = applicationType,
            RequiresSecret = false,
            CreatedAt = DateTime.UtcNow
        };

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var ownerOrganizationId = await GetOwnerOrganizationIdAsync(
            connection,
            transaction,
            ownerId,
            cancellationToken);

        const string QUERY = "INSERT INTO `client` (`id`, `owner_id`, `name`, `application_type`, `created_at`) VALUES (@Id, @OwnerId, @Name, @ApplicationType, @CreatedAt)";
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
            var defaultGroupClaimMapping = GroupClaimMappingPolicy.CreateDefault(ownerOrganizationId);
            var defaultScopes = OidcScopePolicy.DefaultApplicationScopes
                .Select(scope => new ApplicationClaim(id, ScopeClaimName, scope))
                .Append(new ApplicationClaim(
                    id,
                    GroupClaimFormatClaimName,
                    defaultGroupClaimMapping.Format))
                .Concat(defaultGroupClaimMapping.Selectors.Select(selector =>
                    new ApplicationClaim(id, GroupClaimSelectorClaimName, selector)))
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
            "DELETE FROM `oauth_grant` WHERE `client_id` = @id",
            "DELETE FROM `oauth_refresh_token` WHERE `client_id` = @id",
            "DELETE FROM `client_secret` WHERE `client_id` = @id"
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

        return await GetApplicationAsync(id, ownerId, cancellationToken);
    }

    public async Task<OAuthApplicationConfiguration?> GetApplicationAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        return await GetApplicationAsync(id, null, cancellationToken);
    }

    private async Task<OAuthApplicationConfiguration?> GetApplicationAsync(
        string id,
        string? ownerId,
        CancellationToken cancellationToken)
    {

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var applicationQuery = ownerId is null
            ? "SELECT `id`, `owner_id` AS `OwnerId`, `name`, `application_type` AS `ApplicationType`, `requires_secret` AS `RequiresSecret`, `created_at` AS `CreatedAt` FROM `client` WHERE `id` = @id AND `removed_at` IS NULL"
            : "SELECT `id`, `owner_id` AS `OwnerId`, `name`, `application_type` AS `ApplicationType`, `requires_secret` AS `RequiresSecret`, `created_at` AS `CreatedAt` FROM `client` WHERE `id` = @id AND `owner_id` = @ownerId AND `removed_at` IS NULL";
        var command = new CommandDefinition(
            applicationQuery,
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

        command = new CommandDefinition(
            CLAIM_QUERY,
            new { id, name = GroupClaimFormatClaimName },
            cancellationToken: cancellationToken);
        var groupClaimFormat = (await connection.QueryAsync<string>(command)).FirstOrDefault();

        command = new CommandDefinition(
            CLAIM_QUERY,
            new { id, name = GroupClaimSelectorClaimName },
            cancellationToken: cancellationToken);
        var groupClaimSelectors = (await connection.QueryAsync<string>(command)).ToArray();

        GroupClaimMapping groupClaimMapping;
        if (groupClaimFormat is null)
        {
            var ownerOrganizationId = await GetOwnerOrganizationIdAsync(
                connection,
                null,
                application.OwnerId,
                cancellationToken);
            groupClaimMapping = GroupClaimMappingPolicy.CreateDefault(ownerOrganizationId);
        }
        else
        {
            if (!GroupClaimMappingPolicy.TryNormalize(
                    new GroupClaimMapping
                    {
                        Format = groupClaimFormat,
                        Selectors = groupClaimSelectors
                    },
                    out var normalizedGroupClaimMapping,
                    out var mappingError))
            {
                throw new InvalidOperationException(
                    $"Application '{id}' contains an invalid group claim mapping: {mappingError}");
            }

            groupClaimMapping = normalizedGroupClaimMapping;
        }

        return new OAuthApplicationConfiguration
        {
            Application = application,
            RedirectUris = redirectUris,
            AllowedScopes = allowedScopes,
            GroupClaimMapping = groupClaimMapping
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
                `application_type` AS `ApplicationType`,
                `requires_secret` AS `RequiresSecret`,
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
        GroupClaimMapping? groupClaimMapping,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentNullException.ThrowIfNull(redirectUris);
        ArgumentNullException.ThrowIfNull(allowedScopes);
        if (!allowedScopes.Contains(OidcScopePolicy.OpenIdScope, StringComparer.Ordinal))
        {
            throw new ArgumentException("The openid scope is required.", nameof(allowedScopes));
        }

        var hasOrganizationClaimScope = allowedScopes.Contains(
                OidcScopePolicy.GroupsScope,
                StringComparer.Ordinal)
            || allowedScopes.Contains(
                OidcScopePolicy.OrganizationScope,
                StringComparer.Ordinal);
        GroupClaimMapping? normalizedGroupClaimMapping = null;
        if (groupClaimMapping is not null)
        {
            if (!hasOrganizationClaimScope)
            {
                throw new ArgumentException(
                    "A group claim mapping requires the groups or organization scope.",
                    nameof(groupClaimMapping));
            }

            if (!GroupClaimMappingPolicy.TryNormalize(
                    groupClaimMapping,
                    out normalizedGroupClaimMapping,
                    out var mappingError))
            {
                throw new ArgumentException(mappingError, nameof(groupClaimMapping));
            }
        }

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string LOCK_QUERY = "SELECT `application_type` FROM `client` WHERE `id` = @id AND `owner_id` = @ownerId AND `removed_at` IS NULL FOR UPDATE";
        var command = new CommandDefinition(
            LOCK_QUERY,
            new { id, ownerId },
            transaction,
            cancellationToken: cancellationToken);
        var applicationType = await connection.QuerySingleOrDefaultAsync<string>(command);
        if (applicationType is null)
        {
            return false;
        }

        if (redirectUris.Any(value =>
            !OidcRedirectUriPolicy.IsValidRegistration(value, applicationType)))
        {
            throw new ArgumentException(
                "A redirect URI is not valid for the application type.",
                nameof(redirectUris));
        }

        const string REMOVE_CLAIMS_QUERY = "UPDATE `client_claim` SET `removed_at` = @removedAt WHERE `client_id` = @id AND `name` IN @names AND `removed_at` IS NULL";
        command = new CommandDefinition(
            REMOVE_CLAIMS_QUERY,
            new
            {
                id,
                names = new[]
                {
                    RedirectUriClaimName,
                    ScopeClaimName,
                    GroupClaimFormatClaimName,
                    GroupClaimSelectorClaimName
                },
                removedAt = DateTime.UtcNow
            },
            transaction,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);

        var claims = redirectUris
            .Select(value => new ApplicationClaim(id, RedirectUriClaimName, value))
            .Concat(allowedScopes.Select(value => new ApplicationClaim(id, ScopeClaimName, value)))
            .Concat(normalizedGroupClaimMapping is null
                ? []
                : new[]
                    {
                        new ApplicationClaim(
                            id,
                            GroupClaimFormatClaimName,
                            normalizedGroupClaimMapping.Format)
                    }
                    .Concat(normalizedGroupClaimMapping.Selectors.Select(selector =>
                        new ApplicationClaim(id, GroupClaimSelectorClaimName, selector))))
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

    private static async Task<string?> GetOwnerOrganizationIdAsync(
        MySqlConnection connection,
        System.Data.Common.DbTransaction? transaction,
        string ownerId,
        CancellationToken cancellationToken)
    {
        const string QUERY = "SELECT `id` FROM `organization` WHERE @ownerId = CONCAT(@organizationPrefix, LOWER(SHA2(`id`, 256))) LIMIT 1";
        var command = new CommandDefinition(
            QUERY,
            new
            {
                ownerId,
                organizationPrefix = ApplicationOwnerIds.OrganizationPrefix
            },
            transaction,
            cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<string>(command);
    }
}
