using System.Net;
using OAuth2.DataTransfer;

namespace OAuth2.Services;

public sealed record BackendResponse(
    HttpStatusCode StatusCode,
    string? Content,
    string? ContentType);

public sealed record BackendResponse<T>(
    HttpStatusCode StatusCode,
    T? Value,
    string? Content,
    string? ContentType);

public interface IBackendClient
{
    Task<bool> VerifyAccountIdAsync(string id, CancellationToken cancellationToken = default);

    Task<BackendResponse> RegisterAccountAsync(
        RegisterForm form,
        string? acceptLanguage,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> LoginAsync(
        LoginForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> VerifyEmailAsync(
        EmailVerificationForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> ResendEmailVerificationAsync(
        EmailVerificationResendForm form,
        string? acceptLanguage,
        CancellationToken cancellationToken = default);

    Task<BackendResponse<SessionUser>> ExchangeAuthorizationCodeAsync(
        AuthorizationCodeExchange exchange,
        CancellationToken cancellationToken = default);
}
