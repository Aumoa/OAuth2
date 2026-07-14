using OAuth2.DataTransfer;

namespace OAuth2.Services;

public interface IBackendClient
{
    Task<bool> AccountExistsAsync(string id, CancellationToken cancellationToken = default);

    Task<BackendResponse> RegisterAccountAsync(
        RegisterForm form,
        string? acceptLanguage,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> CreateAuthorizationCodeAsync(
        LoginForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> VerifyEmailAsync(
        EmailVerificationForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> CreateEmailVerificationDeliveryAsync(
        EmailVerificationResendForm form,
        string? acceptLanguage,
        CancellationToken cancellationToken = default);

    Task<BackendResponse<GrantedUserInfo>> ExchangeAuthorizationCodeAsync(
        AuthorizationCodeExchange exchange,
        CancellationToken cancellationToken = default);
}
