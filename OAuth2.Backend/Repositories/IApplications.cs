using OAuth2.Data;

namespace OAuth2.Repositories;

public interface IApplications
{
    Task<IReadOnlyList<OAuthApplication>> GetOwnedApplicationsAsync(
        string ownerId,
        CancellationToken cancellationToken = default);
}
