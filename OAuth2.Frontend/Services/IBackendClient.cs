namespace OAuth2.Services;

public interface IBackendClient
{
    Task<bool> VerifyAccountIdAsync(string id, CancellationToken cancellationToken = default);
}
