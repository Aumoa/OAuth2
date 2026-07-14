using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OAuth2.OpenId;
using OAuth2.Options;
using OAuth2.Services;
using StackExchange.Redis;

namespace OAuth2.Repositories;

internal sealed class RedisRememberedSessions(
    RedisConnection connection,
    IOptions<RememberedSessionOptions> options) : IRememberedSessions
{
    private const string KeyPrefix = "oauth2:remembered_session:";
    private static readonly JsonSerializerOptions s_JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async ValueTask<CreatedRememberedSession> CreateAsync(
        string accountId,
        long authTime,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        var lifetime = options.Value.Lifetime;
        var expiresAt = DateTimeOffset.UtcNow.Add(lifetime);
        var value = JsonSerializer.Serialize(
            new RememberedSessionRecord
            {
                AccountId = accountId,
                AuthTime = authTime,
                ExpiresAt = expiresAt
            },
            s_JsonOptions);
        var database = connection.GetDatabase();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var token = Pkce.CreateCodeVerifier();
            var created = await database.StringSetAsync(
                    CreateKey(token),
                    value,
                    lifetime,
                    When.NotExists)
                .WaitAsync(cancellationToken);
            if (created)
            {
                return new CreatedRememberedSession(token, expiresAt);
            }
        }

        throw new InvalidOperationException(
            "Unable to allocate a unique remembered session token.");
    }

    public async ValueTask<RememberedSessionRecord?> GetAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        var database = connection.GetDatabase();
        var key = CreateKey(token);
        var value = await database.StringGetAsync(key).WaitAsync(cancellationToken);
        if (value.IsNullOrEmpty)
        {
            return null;
        }

        RememberedSessionRecord? session;
        try
        {
            session = JsonSerializer.Deserialize<RememberedSessionRecord>(
                value.ToString(),
                s_JsonOptions);
        }
        catch (JsonException)
        {
            session = null;
        }

        if (session is null || session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            await database.KeyDeleteAsync(key).WaitAsync(cancellationToken);
            return null;
        }

        return session;
    }

    public async ValueTask DeleteAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        await connection.GetDatabase()
            .KeyDeleteAsync(CreateKey(token))
            .WaitAsync(cancellationToken);
    }

    private static string CreateKey(string token)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(token));
        return KeyPrefix + Convert.ToHexStringLower(hash);
    }
}
