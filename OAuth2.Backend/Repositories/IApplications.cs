using OAuth2.Data;

namespace OAuth2.Repositories;

public interface IApplications
{
    Task<OAuthApplication?> AddApplicationAsync(
        string id,
        string ownerId,
        string name,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OAuthApplication>> GetOwnedApplicationsAsync(
        string ownerId,
        CancellationToken cancellationToken = default);
}
