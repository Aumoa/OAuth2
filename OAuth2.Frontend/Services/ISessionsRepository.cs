using OAuth2.DataTransfer;

namespace OAuth2.Services;

public interface ISessionsRepository
{
    ValueTask<CreatedSession> CreateAsync(
        SessionUser user,
        CancellationToken cancellationToken = default);

    ValueTask<SessionUser?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}
