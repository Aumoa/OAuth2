using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Data;
using OAuth2.Options;

namespace OAuth2.Repositories;

internal sealed class MySqlApplicationSecrets(IOptions<MySqlOptions> mysqlOptions)
    : IApplicationSecrets
{
    private const string SecretMarker = "o2s_";
    private const int PrefixLength = 12;

    public async Task<GeneratedOAuthApplicationSecret?> AddAsync(
        string clientId,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string LOCK_QUERY = """
            SELECT `application_type`
            FROM `client`
            WHERE `id` = @clientId
                AND `owner_id` = @ownerId
                AND `removed_at` IS NULL
            FOR UPDATE
            """;
        var command = new CommandDefinition(
            LOCK_QUERY,
            new { clientId, ownerId },
            transaction,
            cancellationToken: cancellationToken);
        var applicationType = await connection.QuerySingleOrDefaultAsync<string>(command);
        if (applicationType != OAuthApplicationTypes.Web)
        {
            return null;
        }

        const string COUNT_QUERY = """
            SELECT COUNT(*)
            FROM `client_secret`
            WHERE `client_id` = @clientId
                AND `removed_at` IS NULL
            """;
        command = new CommandDefinition(
            COUNT_QUERY,
            new { clientId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.QuerySingleAsync<int>(command) >= IApplicationSecrets.MaxActiveSecrets)
        {
            return null;
        }

        var value = GenerateSecret();
        var createdAt = DateTime.UtcNow;
        var prefix = value[..PrefixLength];
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        const string INSERT_QUERY = """
            INSERT INTO `client_secret`
                (`client_id`, `secret_prefix`, `secret_hash`, `created_at`)
            VALUES
                (@clientId, @prefix, @hash, @createdAt)
            """;
        command = new CommandDefinition(
            INSERT_QUERY,
            new { clientId, prefix, hash, createdAt },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            return null;
        }

        command = new CommandDefinition(
            "SELECT LAST_INSERT_ID()",
            transaction: transaction,
            cancellationToken: cancellationToken);
        var secretId = await connection.QuerySingleAsync<long>(command);
        const string REQUIRE_SECRET_QUERY = """
            UPDATE `client`
            SET `requires_secret` = TRUE
            WHERE `id` = @clientId
                AND `owner_id` = @ownerId
                AND `removed_at` IS NULL
            """;
        command = new CommandDefinition(
            REQUIRE_SECRET_QUERY,
            new { clientId, ownerId },
            transaction,
            cancellationToken: cancellationToken);
        await connection.ExecuteAsync(command);

        await transaction.CommitAsync(cancellationToken);
        return new GeneratedOAuthApplicationSecret
        {
            ApplicationSecret = new OAuthApplicationSecret
            {
                Id = secretId,
                ClientId = clientId,
                Prefix = prefix,
                CreatedAt = createdAt
            },
            Value = value
        };
    }

    public async Task<bool> DeleteAsync(
        string clientId,
        string ownerId,
        long secretId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(secretId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string QUERY = """
            UPDATE `client_secret` `s`
            INNER JOIN `client` `c`
                ON `c`.`id` = `s`.`client_id`
            SET `s`.`removed_at` = @removedAt
            WHERE `s`.`id` = @secretId
                AND `s`.`client_id` = @clientId
                AND `s`.`removed_at` IS NULL
                AND `c`.`owner_id` = @ownerId
                AND `c`.`removed_at` IS NULL
            """;
        var command = new CommandDefinition(
            QUERY,
            new { clientId, ownerId, secretId, removedAt = DateTime.UtcNow },
            cancellationToken: cancellationToken);
        return await connection.ExecuteAsync(command) == 1;
    }

    public async Task<IReadOnlyList<OAuthApplicationSecret>> GetOwnedAsync(
        string clientId,
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string QUERY = """
            SELECT
                `s`.`id` AS `Id`,
                `s`.`client_id` AS `ClientId`,
                `s`.`secret_prefix` AS `Prefix`,
                `s`.`created_at` AS `CreatedAt`
            FROM `client_secret` `s`
            INNER JOIN `client` `c`
                ON `c`.`id` = `s`.`client_id`
            WHERE `s`.`client_id` = @clientId
                AND `s`.`removed_at` IS NULL
                AND `c`.`owner_id` = @ownerId
                AND `c`.`removed_at` IS NULL
            ORDER BY `s`.`created_at` DESC, `s`.`id` DESC
            """;
        var command = new CommandDefinition(
            QUERY,
            new { clientId, ownerId },
            cancellationToken: cancellationToken);
        var secrets = await connection.QueryAsync<OAuthApplicationSecret>(command);
        return secrets.ToArray();
    }

    public async Task<bool> VerifyAsync(
        string clientId,
        string secret,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < PrefixLength)
        {
            return false;
        }

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        const string QUERY = """
            SELECT `secret_hash`
            FROM `client_secret`
            WHERE `client_id` = @clientId
                AND `secret_prefix` = @prefix
                AND `removed_at` IS NULL
            """;
        var command = new CommandDefinition(
            QUERY,
            new { clientId, prefix = secret[..PrefixLength] },
            cancellationToken: cancellationToken);
        var savedHashes = await connection.QueryAsync<byte[]>(command);
        var computedHash = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
        var verified = false;
        foreach (var savedHash in savedHashes)
        {
            verified |= CryptographicOperations.FixedTimeEquals(computedHash, savedHash);
        }

        return verified;
    }

    private static string GenerateSecret()
    {
        var randomValue = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        return SecretMarker + randomValue;
    }
}
