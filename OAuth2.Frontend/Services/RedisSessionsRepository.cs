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
    private const int MaxUpdateAttempts = 8;
    private static readonly JsonSerializerOptions s_JsonOptions =
        new(JsonSerializerDefaults.Web);

    public async ValueTask<CreatedSession> CreateAsync(
        GrantedUserInfo userInfo,
        string sessionScope,
        string? currentSessionId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userInfo);
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionScope);

        if (!OidcScopePolicy.TryCombine(userInfo.Scope, sessionScope, out _))
        {
            throw new InvalidOperationException(
                "The session scope cannot exceed the scope granted by the backend.");
        }

        var subject = GetStringClaim(userInfo.Claims, "sub");
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException(
                "The granted user information must contain a subject claim.");
        }

        if (!string.IsNullOrWhiteSpace(currentSessionId))
        {
            var updated = await TryUpdateExistingAsync(
                currentSessionId,
                userInfo,
                sessionScope,
                subject,
                cancellationToken);
            if (updated.HasValue)
            {
                return updated.Value;
            }
        }

        return await CreateNewAsync(
            userInfo,
            sessionScope,
            subject,
            cancellationToken);
    }

    public async ValueTask<SessionRecord?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var record = await ReadAsync(sessionId, cancellationToken);
        if (record is null
            || string.IsNullOrWhiteSpace(record.ActiveAccountKey)
            || !record.ActiveExpiresAt.HasValue
            || record.ActiveExpiresAt <= DateTimeOffset.UtcNow)
        {
            return null;
        }

        var account = record.Accounts.FirstOrDefault(item =>
            string.Equals(item.Key, record.ActiveAccountKey, StringComparison.Ordinal));
        if (account is null)
        {
            return null;
        }

        return new SessionRecord
        {
            GrantedScope = account.GrantedScope,
            SessionScope = account.SessionScope,
            Claims = account.Claims
        };
    }

    public async ValueTask<IReadOnlyList<RememberedAccount>> GetRememberedAccountsAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var record = await ReadAsync(sessionId, cancellationToken);
        if (record is null)
        {
            return [];
        }

        var now = DateTimeOffset.UtcNow;
        return record.Accounts
            .OrderByDescending(static account => account.LastUsedAt)
            .Select(account => CreateRememberedAccount(account, now))
            .Where(static account => account is not null)
            .Cast<RememberedAccount>()
            .ToArray();
    }

    public async ValueTask<RememberedAccountCredential?> GetRememberedAccountCredentialAsync(
        string sessionId,
        string accountKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountKey);

        var record = await ReadAsync(sessionId, cancellationToken);
        var account = record?.Accounts.FirstOrDefault(item =>
            string.Equals(item.Key, accountKey, StringComparison.Ordinal));
        if (account is null)
        {
            return null;
        }

        var id = GetStringClaim(account.Claims, "preferred_username");
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var token = account.TokenExpiresAt > DateTimeOffset.UtcNow
            ? account.Token
            : null;
        return new RememberedAccountCredential
        {
            Id = id,
            Token = token
        };
    }

    public async ValueTask<RemovedRememberedAccount> RemoveRememberedAccountAsync(
        string sessionId,
        string accountKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountKey);

        var database = connection.GetDatabase();
        var key = CreateKey(sessionId);

        for (var attempt = 0; attempt < MaxUpdateAttempts; attempt++)
        {
            var value = await database.StringGetAsync(key).WaitAsync(cancellationToken);
            if (value.IsNullOrEmpty || !TryDeserialize(value, out var record))
            {
                return new RemovedRememberedAccount(false, null, false);
            }

            var removed = record.Accounts.FirstOrDefault(account =>
                string.Equals(account.Key, accountKey, StringComparison.Ordinal));
            if (removed is null)
            {
                return new RemovedRememberedAccount(false, null, record.Accounts.Count > 0);
            }

            var accounts = record.Accounts
                .Where(account => !string.Equals(
                    account.Key,
                    accountKey,
                    StringComparison.Ordinal))
                .ToList();
            var updated = record with
            {
                ActiveAccountKey = string.Equals(
                    record.ActiveAccountKey,
                    accountKey,
                    StringComparison.Ordinal)
                    ? null
                    : record.ActiveAccountKey,
                ActiveExpiresAt = string.Equals(
                    record.ActiveAccountKey,
                    accountKey,
                    StringComparison.Ordinal)
                    ? null
                    : record.ActiveExpiresAt,
                Accounts = accounts
            };

            var transaction = database.CreateTransaction();
            transaction.AddCondition(Condition.StringEqual(key, value));
            if (accounts.Count == 0)
            {
                _ = transaction.KeyDeleteAsync(key);
            }
            else
            {
                _ = transaction.StringSetAsync(
                    key,
                    Serialize(updated),
                    RemainingLifetime(updated));
            }

            if (await transaction.ExecuteAsync().WaitAsync(cancellationToken))
            {
                return new RemovedRememberedAccount(
                    true,
                    removed.Token,
                    accounts.Count > 0);
            }
        }

        throw new InvalidOperationException(
            "The browser session changed too frequently to remove an account.");
    }

    public async ValueTask<bool> SignOutAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        var database = connection.GetDatabase();
        var key = CreateKey(sessionId);

        for (var attempt = 0; attempt < MaxUpdateAttempts; attempt++)
        {
            var value = await database.StringGetAsync(key).WaitAsync(cancellationToken);
            if (value.IsNullOrEmpty || !TryDeserialize(value, out var record))
            {
                return false;
            }

            if (record.ActiveAccountKey is null)
            {
                return true;
            }

            var updated = record with
            {
                ActiveAccountKey = null,
                ActiveExpiresAt = null
            };
            var transaction = database.CreateTransaction();
            transaction.AddCondition(Condition.StringEqual(key, value));
            _ = transaction.StringSetAsync(
                key,
                Serialize(updated),
                RemainingLifetime(updated));
            if (await transaction.ExecuteAsync().WaitAsync(cancellationToken))
            {
                return true;
            }
        }

        throw new InvalidOperationException(
            "The browser session changed too frequently to sign out.");
    }

    public ValueTask<bool> UpdateActiveAccountClaimAsync(
        string sessionId,
        string claimName,
        JsonElement? value,
        CancellationToken cancellationToken = default) =>
        UpdateActiveAccountClaimsAsync(
            sessionId,
            new Dictionary<string, JsonElement?>(StringComparer.Ordinal)
            {
                [claimName] = value
            },
            cancellationToken);

    public async ValueTask<bool> UpdateActiveAccountClaimsAsync(
        string sessionId,
        IReadOnlyDictionary<string, JsonElement?> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(values);
        if (values.Keys.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Claim names cannot be empty.", nameof(values));
        }

        var database = connection.GetDatabase();
        var key = CreateKey(sessionId);

        for (var attempt = 0; attempt < MaxUpdateAttempts; attempt++)
        {
            var savedValue = await database.StringGetAsync(key).WaitAsync(cancellationToken);
            if (savedValue.IsNullOrEmpty || !TryDeserialize(savedValue, out var record))
            {
                return false;
            }

            var activeIndex = record.Accounts.FindIndex(account => string.Equals(
                account.Key,
                record.ActiveAccountKey,
                StringComparison.Ordinal));
            if (activeIndex < 0)
            {
                return false;
            }

            var account = record.Accounts[activeIndex];
            var claims = new Dictionary<string, JsonElement>(account.Claims, StringComparer.Ordinal);
            foreach (var (claimName, value) in values)
            {
                if (value.HasValue)
                {
                    claims[claimName] = value.Value.Clone();
                }
                else
                {
                    claims.Remove(claimName);
                }
            }

            var accounts = record.Accounts.ToList();
            accounts[activeIndex] = account with { Claims = claims };
            var updated = record with { Accounts = accounts };
            var transaction = database.CreateTransaction();
            transaction.AddCondition(Condition.StringEqual(key, savedValue));
            _ = transaction.StringSetAsync(
                key,
                Serialize(updated),
                RemainingLifetime(updated));
            if (await transaction.ExecuteAsync().WaitAsync(cancellationToken))
            {
                return true;
            }
        }

        throw new InvalidOperationException(
            "The browser session changed too frequently to update an account claim.");
    }

    private async ValueTask<CreatedSession?> TryUpdateExistingAsync(
        string sessionId,
        GrantedUserInfo userInfo,
        string sessionScope,
        string subject,
        CancellationToken cancellationToken)
    {
        var database = connection.GetDatabase();
        var key = CreateKey(sessionId);

        for (var attempt = 0; attempt < MaxUpdateAttempts; attempt++)
        {
            var value = await database.StringGetAsync(key).WaitAsync(cancellationToken);
            if (value.IsNullOrEmpty || !TryDeserialize(value, out var current))
            {
                return null;
            }

            var update = UpdateRecord(current, userInfo, sessionScope, subject);
            var transaction = database.CreateTransaction();
            transaction.AddCondition(Condition.StringEqual(key, value));
            _ = transaction.StringSetAsync(
                key,
                Serialize(update.Record),
                RemainingLifetime(update.Record));
            if (await transaction.ExecuteAsync().WaitAsync(cancellationToken))
            {
                return new CreatedSession(
                    sessionId,
                    update.Record.ExpiresAt,
                    update.SupersededTokens);
            }
        }

        throw new InvalidOperationException(
            "The browser session changed too frequently to complete sign-in.");
    }

    private async ValueTask<CreatedSession> CreateNewAsync(
        GrantedUserInfo userInfo,
        string sessionScope,
        string subject,
        CancellationToken cancellationToken)
    {
        var database = connection.GetDatabase();

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var sessionId = Pkce.CreateCodeVerifier();
            var update = UpdateRecord(null, userInfo, sessionScope, subject);
            var created = await database.StringSetAsync(
                    CreateKey(sessionId),
                    Serialize(update.Record),
                    RemainingLifetime(update.Record),
                    When.NotExists)
                .WaitAsync(cancellationToken);
            if (created)
            {
                return new CreatedSession(
                    sessionId,
                    update.Record.ExpiresAt,
                    update.SupersededTokens);
            }
        }

        throw new InvalidOperationException(
            "Unable to allocate a unique browser session identifier.");
    }

    private SessionUpdate UpdateRecord(
        BrowserSessionRecord? current,
        GrantedUserInfo userInfo,
        string sessionScope,
        string subject)
    {
        var now = DateTimeOffset.UtcNow;
        var accounts = current?.Accounts.ToList() ?? [];
        var existingIndex = accounts.FindIndex(account => string.Equals(
            GetStringClaim(account.Claims, "sub"),
            subject,
            StringComparison.Ordinal));
        var existing = existingIndex >= 0 ? accounts[existingIndex] : null;
        var grant = userInfo.RememberedSession;
        var token = grant?.Token ?? existing?.Token;
        var tokenExpiresAt = grant?.ExpiresAt ?? existing?.TokenExpiresAt;
        var authenticatedAt = grant?.AuthenticatedAt
            ?? existing?.AuthenticatedAt
            ?? now;
        var accountKey = existing?.Key ?? Pkce.CreateCodeVerifier();
        var supersededTokens = new HashSet<string>(StringComparer.Ordinal);

        if (grant is not null
            && !string.IsNullOrWhiteSpace(existing?.Token)
            && !string.Equals(existing.Token, grant.Token, StringComparison.Ordinal))
        {
            supersededTokens.Add(existing.Token);
        }

        var account = new RememberedAccountRecord
        {
            Key = accountKey,
            GrantedScope = userInfo.Scope,
            SessionScope = sessionScope,
            Claims = userInfo.Claims,
            Token = token,
            TokenExpiresAt = tokenExpiresAt,
            AuthenticatedAt = authenticatedAt,
            LastUsedAt = now
        };
        if (existingIndex >= 0)
        {
            accounts[existingIndex] = account;
        }
        else
        {
            accounts.Add(account);
        }

        while (accounts.Count > sessionOptions.Value.MaxRememberedAccounts)
        {
            var evicted = accounts
                .Where(item => !string.Equals(item.Key, accountKey, StringComparison.Ordinal))
                .MinBy(static item => item.LastUsedAt)
                ?? throw new InvalidOperationException(
                    "Unable to choose a remembered account to evict.");
            accounts.Remove(evicted);
            if (!string.IsNullOrWhiteSpace(evicted.Token))
            {
                supersededTokens.Add(evicted.Token);
            }
        }

        var activeExpiresAt = now.Add(sessionOptions.Value.Lifetime);
        if (tokenExpiresAt.HasValue && tokenExpiresAt < activeExpiresAt)
        {
            activeExpiresAt = tokenExpiresAt.Value;
        }

        var record = new BrowserSessionRecord
        {
            ActiveAccountKey = accountKey,
            ActiveExpiresAt = activeExpiresAt,
            ExpiresAt = now.Add(sessionOptions.Value.BrowserLifetime),
            Accounts = accounts
        };
        return new SessionUpdate(record, supersededTokens.ToArray());
    }

    private async ValueTask<BrowserSessionRecord?> ReadAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        var value = await connection.GetDatabase()
            .StringGetAsync(CreateKey(sessionId))
            .WaitAsync(cancellationToken);
        return value.IsNullOrEmpty || !TryDeserialize(value, out var record)
            ? null
            : record;
    }

    private static RememberedAccount? CreateRememberedAccount(
        RememberedAccountRecord account,
        DateTimeOffset now)
    {
        var id = GetStringClaim(account.Claims, "preferred_username");
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return new RememberedAccount
        {
            AccountKey = account.Key,
            Id = id,
            Name = GetStringClaim(account.Claims, "name"),
            Email = GetStringClaim(account.Claims, "email"),
            Picture = GetStringClaim(account.Claims, "picture"),
            CanSignIn = !string.IsNullOrWhiteSpace(account.Token)
                && account.TokenExpiresAt > now,
            AuthenticatedAt = account.AuthenticatedAt
        };
    }

    private static string? GetStringClaim(
        IReadOnlyDictionary<string, JsonElement> claims,
        string name)
    {
        return claims.TryGetValue(name, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static bool TryDeserialize(
        RedisValue value,
        out BrowserSessionRecord record)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<BrowserSessionRecord>(
                value.ToString(),
                s_JsonOptions);
            if (parsed is not null && parsed.Accounts is not null)
            {
                record = parsed;
                return true;
            }
        }
        catch (JsonException)
        {
        }

        record = null!;
        return false;
    }

    private static string Serialize(BrowserSessionRecord record)
    {
        return JsonSerializer.Serialize(record, s_JsonOptions);
    }

    private static TimeSpan RemainingLifetime(BrowserSessionRecord record)
    {
        var lifetime = record.ExpiresAt - DateTimeOffset.UtcNow;
        return lifetime > TimeSpan.Zero ? lifetime : TimeSpan.FromSeconds(1);
    }

    private static string CreateKey(string sessionId)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(sessionId));
        return KeyPrefix + Convert.ToHexStringLower(hash);
    }

    private readonly record struct SessionUpdate(
        BrowserSessionRecord Record,
        IReadOnlyList<string> SupersededTokens);
}
