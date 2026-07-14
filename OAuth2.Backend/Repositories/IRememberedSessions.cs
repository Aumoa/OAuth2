namespace OAuth2.Repositories;

public interface IRememberedSessions
{
    ValueTask<CreatedRememberedSession> CreateAsync(
        string accountId,
        long authTime,
        CancellationToken cancellationToken = default);

    ValueTask<RememberedSessionRecord?> GetAsync(
        string token,
        CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(
        string token,
        CancellationToken cancellationToken = default);
}
