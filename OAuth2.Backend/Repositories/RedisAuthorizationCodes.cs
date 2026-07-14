using System.Globalization;
using System.Security.Cryptography;
using OAuth2.Services;
using StackExchange.Redis;

namespace OAuth2.Repositories;

internal sealed class RedisAuthorizationCodes(
    RedisConnection connection,
    ILogger<RedisAuthorizationCodes> logger) : IAuthorizationCodes
{
    private const string KeyPrefix = "oauth2:authorization_code:";

    private const string PopScript = """
        if redis.call('EXISTS', KEYS[1]) == 0 then
            return nil
        end

        local values = redis.call('HMGET', KEYS[1],
            'account_id',
            'client_id',
            'scope',
            'redirect_uri',
            'nonce',
            'code_challenge',
            'code_challenge_method',
            'auth_time',
            'create_remembered_session')

        redis.call('DEL', KEYS[1])
        return values
        """;

    public async ValueTask<string> PushAsync(
        AuthorizationCodeBody body,
        CancellationToken cancellationToken = default)
    {
        var code = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var key = KeyPrefix + code;
        HashEntry[] entries =
        [
            new("account_id", body.AccountId),
            new("client_id", body.ClientId),
            new("scope", body.Scope),
            new("redirect_uri", body.RedirectUri),
            new("nonce", body.Nonce ?? string.Empty),
            new("code_challenge", body.CodeChallenge ?? string.Empty),
            new("code_challenge_method", body.CodeChallengeMethod ?? string.Empty),
            new("auth_time", body.AuthTime.ToString(CultureInfo.InvariantCulture)),
            new("create_remembered_session", body.CreateRememberedSession ? "1" : "0")
        ];

        var database = connection.GetDatabase();
        await database.HashSetAsync(key, entries).WaitAsync(cancellationToken);
        await database.KeyExpireAsync(key, TimeSpan.FromMinutes(5)).WaitAsync(cancellationToken);
        return code;
    }

    public async ValueTask<AuthorizationCodeBody?> PopAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var result = await connection.GetDatabase()
            .ScriptEvaluateAsync(PopScript, [KeyPrefix + code])
            .WaitAsync(cancellationToken);
        if (result.IsNull)
        {
            return null;
        }

        var values = (RedisResult[]?)result;
        if (values is not { Length: 9 })
        {
            logger.LogError("Invalid authorization code record was read from Redis.");
            return null;
        }

        var accountId = Read(values[0]);
        var clientId = Read(values[1]);
        var scope = Read(values[2]);
        var redirectUri = Read(values[3]);
        var nonce = Read(values[4]);
        var codeChallenge = Read(values[5]);
        var codeChallengeMethod = Read(values[6]);
        var authTimeValue = Read(values[7]);
        var createRememberedSessionValue = Read(values[8]);

        if (string.IsNullOrWhiteSpace(accountId)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(scope)
            || string.IsNullOrWhiteSpace(redirectUri)
            || !long.TryParse(authTimeValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out var authTime))
        {
            logger.LogError("Authorization code record contained invalid data.");
            return null;
        }

        return new AuthorizationCodeBody(
            accountId,
            clientId,
            scope,
            redirectUri,
            EmptyToNull(nonce),
            EmptyToNull(codeChallenge),
            EmptyToNull(codeChallengeMethod),
            authTime,
            string.Equals(createRememberedSessionValue, "1", StringComparison.Ordinal));
    }

    private static string? Read(RedisResult value)
    {
        return value.IsNull ? null : ((RedisValue)value).ToString();
    }

    private static string? EmptyToNull(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
