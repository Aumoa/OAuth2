using OAuth2.Data;

namespace OAuth2.Repositories;

public interface IApplicationSecrets
{
    const int MaxActiveSecrets = 10;

    Task<GeneratedOAuthApplicationSecret?> AddAsync(
        string clientId,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string clientId,
        string ownerId,
        long secretId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OAuthApplicationSecret>> GetOwnedAsync(
        string clientId,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyAsync(
        string clientId,
        string secret,
        CancellationToken cancellationToken = default);
}
