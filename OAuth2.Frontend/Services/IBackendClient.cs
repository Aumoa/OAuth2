using System.Net;
using OAuth2.DataTransfer;

namespace OAuth2.Services;

public interface IBackendClient
{
    Task<bool> VerifyAccountIdAsync(string id, CancellationToken cancellationToken = default);

    Task<HttpStatusCode> RegisterAccountAsync(
        RegisterForm form,
        string? acceptLanguage,
        CancellationToken cancellationToken = default);
}
