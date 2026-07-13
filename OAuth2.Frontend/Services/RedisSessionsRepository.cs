using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Options;
using StackExchange.Redis;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Services;

internal sealed class RedisSessionsRepository(
    RedisConnection connection,
    IOptions<BffSessionOptions> sessionOptions) : ISessionsRepository
{
    private const string KeyPrefix = "oauth2:frontend:session:";
    private static readonly JsonSerializerOptions s_JsonOptions = new(JsonSerializerDefaults.Web);

    public async ValueTask<CreatedSession> CreateAsync(
        SessionUser user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var database = connection.GetDatabase();
        var value = JsonSerializer.Serialize(user, s_JsonOptions);
        var lifetime = sessionOptions.Value.Lifetime;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var sessionId = Pkce.CreateCodeVerifier();
            var created = await database.StringSetAsync(
                    CreateKey(sessionId),
                    value,
                    lifetime,
                    When.NotExists)
                .WaitAsync(cancellationToken);
            if (created)
            {
                return new CreatedSession(sessionId, DateTimeOffset.UtcNow.Add(lifetime));
            }
        }

        throw new InvalidOperationException("Unable to allocate a unique session identifier.");
    }

    public async ValueTask<SessionUser?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var value = await connection.GetDatabase()
            .StringGetAsync(CreateKey(sessionId))
            .WaitAsync(cancellationToken);
        return value.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<SessionUser>(value.ToString(), s_JsonOptions);
    }

    public async ValueTask DeleteAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        await connection.GetDatabase()
            .KeyDeleteAsync(CreateKey(sessionId))
            .WaitAsync(cancellationToken);
    }

    private static string CreateKey(string sessionId)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(sessionId));
        return KeyPrefix + Convert.ToHexStringLower(hash);
    }
}
