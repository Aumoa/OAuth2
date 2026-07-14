using OAuth2.DataTransfer;

namespace OAuth2.Services;

public interface ISessionsRepository
{
    ValueTask<CreatedSession> CreateAsync(
        GrantedUserInfo userInfo,
        string sessionScope,
        CancellationToken cancellationToken = default);

    ValueTask<SessionRecord?> GetAsync(
        string sessionId,
        CancellationToken cancellationToken = default);

    ValueTask DeleteAsync(
        string sessionId,
        CancellationToken cancellationToken = default);
}
