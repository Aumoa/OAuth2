using OAuth2.DataTransfer;

namespace OAuth2.Services;

public interface IBackendClient
{
    Task<bool> AccountExistsAsync(string id, CancellationToken cancellationToken = default);

    Task<BackendResponse> CreateApplicationAsync(
        string ownerId,
        CreateApplicationForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> CreateOrganizationAsync(
        string accountId,
        CreateOrganizationForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> DeleteApplicationAsync(
        string ownerId,
        string id,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> GetOwnedApplicationAsync(
        string ownerId,
        string id,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> GetOwnedApplicationsAsync(
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> GetOrganizationAsync(
        string accountId,
        string id,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> GetOrganizationsAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> UpdateApplicationAsync(
        string ownerId,
        string id,
        UpdateApplicationForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> RegisterAccountAsync(
        RegisterForm form,
        string? acceptLanguage,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> CreateAuthorizationCodeAsync(
        LoginForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> CreateAuthorizationCodeFromRememberedSessionAsync(
        RememberedLoginForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> RevokeRememberedSessionAsync(
        string token,
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
