using OAuth2.Data;

namespace OAuth2.Repositories;

public interface IApplications
{
    Task<OAuthApplication?> AddApplicationAsync(
        string id,
        string ownerId,
        string name,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteApplicationAsync(
        string id,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<OAuthApplicationConfiguration?> GetOwnedApplicationAsync(
        string id,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OAuthApplication>> GetOwnedApplicationsAsync(
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateApplicationAsync(
        string id,
        string ownerId,
        IReadOnlyCollection<string> redirectUris,
        IReadOnlyCollection<string> allowedScopes,
        CancellationToken cancellationToken = default);
}
