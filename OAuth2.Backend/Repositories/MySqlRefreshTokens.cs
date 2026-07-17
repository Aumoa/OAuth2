using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.OpenId;
using OAuth2.Options;

namespace OAuth2.Repositories;

internal sealed class MySqlRefreshTokens(
    IOptions<MySqlOptions> mysqlOptions,
    IOptions<OidcProviderOptions> oidcOptions) : IRefreshTokens
{
    private const string TokenMarker = "o2r_";
    private const int PrefixLength = 12;

    public async Task<string> CreateAsync(
        string accountId,
        string clientId,
        string scope,
        long authTime,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        var token = GenerateToken();
        var now = DateTime.UtcNow;
        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await InsertAsync(
            connection,
            null,
            Guid.NewGuid().ToByteArray(),
            accountId,
            clientId,
            scope,
            authTime,
            token,
            now,
            cancellationToken);
        return token;
    }

    public async Task<RefreshTokenRotationResult> RotateAsync(
        string clientId,
        string token,
        string? requestedScope,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        if (string.IsNullOrWhiteSpace(token)
            || token.Length < PrefixLength
            || token.Length > IRefreshTokens.MaxTokenLength
            || requestedScope?.Length > IRefreshTokens.MaxScopeLength)
        {
            return InvalidGrant();
        }

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var row = await FindForUpdateAsync(
            connection,
            transaction,
            clientId,
            token,
            cancellationToken);
        if (row is null)
        {
            return InvalidGrant();
        }

        var now = DateTime.UtcNow;
        if (row.ConsumedAt is not null || row.RevokedAt is not null || row.ExpiresAt <= now)
        {
            await RevokeFamilyAsync(
                connection,
                transaction,
                row.FamilyId,
                now,
                cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return InvalidGrant();
        }

        if (!OidcScopePolicy.TryResolveRefreshScope(
                row.Scope,
                requestedScope,
                out var effectiveScope,
                out var replacementGrantScope))
        {
            return new RefreshTokenRotationResult
            {
                Status = RefreshTokenRotationStatus.InvalidScope
            };
        }

        var replacementToken = GenerateToken();
        var replacementId = await InsertAsync(
            connection,
            transaction,
            row.FamilyId,
            row.AccountId,
            row.ClientId,
            replacementGrantScope,
            row.AuthTime,
            replacementToken,
            now,
            cancellationToken);
        const string CONSUME_QUERY = """
            UPDATE `oauth_refresh_token`
            SET `consumed_at` = @now,
                `replacement_id` = @replacementId
            WHERE `id` = @id
                AND `consumed_at` IS NULL
                AND `revoked_at` IS NULL
            """;
        var command = new CommandDefinition(
            CONSUME_QUERY,
            new { row.Id, now, replacementId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return InvalidGrant();
        }

        await transaction.CommitAsync(cancellationToken);
        return new RefreshTokenRotationResult
        {
            Status = RefreshTokenRotationStatus.Succeeded,
            Token = replacementToken,
            AccountId = row.AccountId,
            ClientId = row.ClientId,
            Scope = effectiveScope,
            GrantedScope = replacementGrantScope,
            AuthTime = row.AuthTime
        };
    }

    public async Task RevokeAsync(
        string clientId,
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        if (string.IsNullOrWhiteSpace(token)
            || token.Length < PrefixLength
            || token.Length > IRefreshTokens.MaxTokenLength)
        {
            return;
        }

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var row = await FindForUpdateAsync(
            connection,
            transaction,
            clientId,
            token,
            cancellationToken);
        if (row is not null)
        {
            await RevokeFamilyAsync(
                connection,
                transaction,
                row.FamilyId,
                DateTime.UtcNow,
                cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<long> InsertAsync(
        MySqlConnection connection,
        System.Data.Common.DbTransaction? transaction,
        byte[] familyId,
        string accountId,
        string clientId,
        string scope,
        long authTime,
        string token,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        const string INSERT_QUERY = """
            INSERT INTO `oauth_refresh_token`
                (`family_id`, `account_id`, `client_id`, `scope`, `token_prefix`,
                 `token_hash`, `auth_time`, `created_at`, `expires_at`)
            VALUES
                (@familyId, @accountId, @clientId, @scope, @prefix,
                 @hash, @authTime, @now, @expiresAt)
            """;
        var command = new CommandDefinition(
            INSERT_QUERY,
            new
            {
                familyId,
                accountId,
                clientId,
                scope,
                prefix = token[..PrefixLength],
                hash,
                authTime,
                now,
                expiresAt = now.AddDays(oidcOptions.Value.RefreshTokenLifetimeDays)
            },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            throw new InvalidOperationException("Failed to persist a refresh token.");
        }

        command = new CommandDefinition(
            "SELECT LAST_INSERT_ID()",
            transaction: transaction,
            cancellationToken: cancellationToken);
        return await connection.QuerySingleAsync<long>(command);
    }

    private static async Task<RefreshTokenRow?> FindForUpdateAsync(
        MySqlConnection connection,
        System.Data.Common.DbTransaction transaction,
        string clientId,
        string token,
        CancellationToken cancellationToken)
    {
        const string QUERY = """
            SELECT
                `id` AS `Id`,
                `family_id` AS `FamilyId`,
                `account_id` AS `AccountId`,
                `client_id` AS `ClientId`,
                `scope` AS `Scope`,
                `token_hash` AS `TokenHash`,
                `auth_time` AS `AuthTime`,
                `expires_at` AS `ExpiresAt`,
                `consumed_at` AS `ConsumedAt`,
                `revoked_at` AS `RevokedAt`
            FROM `oauth_refresh_token`
            WHERE `client_id` = @clientId
                AND `token_prefix` = @prefix
            FOR UPDATE
            """;
        var command = new CommandDefinition(
            QUERY,
            new { clientId, prefix = token[..PrefixLength] },
            transaction,
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RefreshTokenRow>(command);
        var computedHash = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        RefreshTokenRow? match = null;
        foreach (var row in rows)
        {
            if (CryptographicOperations.FixedTimeEquals(computedHash, row.TokenHash))
            {
                match = row;
            }
        }

        return match;
    }

    private static async Task RevokeFamilyAsync(
        MySqlConnection connection,
        System.Data.Common.DbTransaction transaction,
        byte[] familyId,
        DateTime revokedAt,
        CancellationToken cancellationToken)
    {
        const string QUERY = """
            UPDATE `oauth_refresh_token`
            SET `revoked_at` = @revokedAt
            WHERE `family_id` = @familyId
                AND `revoked_at` IS NULL
            """;
        var command = new CommandDefinition(
            QUERY,
            new { familyId, revokedAt },
            transaction,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);
    }

    private static RefreshTokenRotationResult InvalidGrant() =>
        new()
        {
            Status = RefreshTokenRotationStatus.InvalidGrant
        };

    private static string GenerateToken()
    {
        var randomValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return TokenMarker + randomValue;
    }

    private sealed record RefreshTokenRow
    {
        public long Id { get; init; }

        public required byte[] FamilyId { get; init; }

        public required string AccountId { get; init; }

        public required string ClientId { get; init; }

        public required string Scope { get; init; }

        public required byte[] TokenHash { get; init; }

        public long AuthTime { get; init; }

        public DateTime ExpiresAt { get; init; }

        public DateTime? ConsumedAt { get; init; }

        public DateTime? RevokedAt { get; init; }
    }
}
