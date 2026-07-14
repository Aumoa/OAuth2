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
        GrantedUserInfo userInfo,
        string sessionScope,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionScope);

        if (!OidcScopePolicy.TryCombine(userInfo.Scope, sessionScope, out _))
        {
            throw new InvalidOperationException(
                "The session scope cannot exceed the scope granted by the backend.");
        }

        var database = connection.GetDatabase();
        var value = JsonSerializer.Serialize(
            new SessionRecord
            {
                GrantedScope = userInfo.Scope,
                SessionScope = sessionScope,
                Claims = userInfo.Claims
            },
            s_JsonOptions);
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

    public async ValueTask<SessionRecord?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var value = await connection.GetDatabase()
            .StringGetAsync(CreateKey(sessionId))
            .WaitAsync(cancellationToken);
        return value.IsNullOrEmpty
            ? null
            : JsonSerializer.Deserialize<SessionRecord>(value.ToString(), s_JsonOptions);
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
