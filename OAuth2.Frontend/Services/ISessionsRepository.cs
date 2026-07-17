using OAuth2.DataTransfer;

namespace OAuth2.Services;

public interface ISessionsRepository
{
    ValueTask<CreatedSession> CreateAsync(
        GrantedUserInfo userInfo,
        string sessionScope,
        string? currentSessionId = null,
        CancellationToken cancellationToken = default);

    ValueTask<SessionRecord?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<RememberedAccount>> GetRememberedAccountsAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    ValueTask<RememberedAccountCredential?> GetRememberedAccountCredentialAsync(
        string sessionId,
        string accountKey,
        CancellationToken cancellationToken = default);

    ValueTask<RemovedRememberedAccount> RemoveRememberedAccountAsync(
        string sessionId,
        string accountKey,
        CancellationToken cancellationToken = default);

    ValueTask<bool> UpdateActiveAccountClaimAsync(
        string sessionId,
        string claimName,
        System.Text.Json.JsonElement? value,
        CancellationToken cancellationToken = default);

    ValueTask<bool> SignOutAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}
