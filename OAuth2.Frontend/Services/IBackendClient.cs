using OAuth2.DataTransfer;

namespace OAuth2.Services;

public interface IBackendClient
{
    Task<bool> AccountExistsAsync(string id, CancellationToken cancellationToken = default);

    Task<BackendResponse> CreateApplicationAsync(
        string ownerId,
        CreateApplicationForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> CreateApplicationSecretAsync(
        string ownerId,
        string clientId,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> CreateOrganizationAsync(
        string accountId,
        CreateOrganizationForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> AddOrganizationMemberAsync(
        string actorAccountId,
        string organizationId,
        AddOrganizationMemberForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> DeleteApplicationAsync(
        string ownerId,
        string id,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> DeleteApplicationSecretAsync(
        string ownerId,
        string clientId,
        long secretId,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> DeleteOrganizationMemberAsync(
        string actorAccountId,
        string organizationId,
        string accountId,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> DeleteOrganizationAsync(
        string actorAccountId,
        string organizationId,
        DeleteOrganizationForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> GetOwnedApplicationAsync(
        string ownerId,
        string id,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> GetOwnedApplicationsAsync(
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> GetApplicationSecretsAsync(
        string ownerId,
        string clientId,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> GetOrganizationAsync(
        string accountId,
        string id,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> GetOrganizationsAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> GetOrganizationMembersAsync(
        string actorAccountId,
        string organizationId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> TransferOrganizationOwnershipAsync(
        string actorAccountId,
        string organizationId,
        TransferOrganizationOwnershipForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> UpdateApplicationAsync(
        string ownerId,
        string id,
        UpdateApplicationForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> UpdateOrganizationMemberAsync(
        string actorAccountId,
        string organizationId,
        string accountId,
        UpdateOrganizationMemberForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> RegisterAccountAsync(
        RegisterForm form,
        string? acceptLanguage,
        CancellationToken cancellationToken = default);

    Task<BackendResponse<LoginResponse>> CreateAuthorizationCodeAsync(
        LoginForm form,
        CancellationToken cancellationToken = default);

    Task<BackendResponse<OidcAuthorizationValidation>> ValidateOidcAuthorizationAsync(
        OpenId.OidcAuthorizationRequest authorization,
        CancellationToken cancellationToken = default);

    Task<BackendResponse<OidcTokenResponse>> ExchangeOidcAuthorizationCodeAsync(
        OidcTokenExchange exchange,
        CancellationToken cancellationToken = default);

    Task<BackendResponse<OidcTokenResponse>> ExchangeOidcRefreshTokenAsync(
        OidcRefreshTokenExchange exchange,
        CancellationToken cancellationToken = default);

    Task<BackendResponse> RevokeOidcTokenAsync(
        OidcTokenRevocation revocation,
        CancellationToken cancellationToken = default);

    Task<BackendResponse<Dictionary<string, System.Text.Json.JsonElement>>> GetOidcUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    Task<BackendResponse<OidcJsonWebKeySet>> GetOidcJsonWebKeysAsync(
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
