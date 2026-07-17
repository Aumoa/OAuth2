using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Options;
using OAuth2.Repositories;

namespace OAuth2.Services;

public sealed class OidcAuthorizationRequestValidator(
    IApplications applications,
    IOptions<OAuthOptions> oauthOptions)
{
    public async Task<OidcAuthorizationValidation> ValidateAsync(
        OidcAuthorizationRequest? request,
        CancellationToken cancellationToken = default)
    {
        if (request is not null
            && string.Equals(
                request.ClientId,
                oauthOptions.Value.ClientId,
                StringComparison.Ordinal))
        {
            var valid = InternalOidcAuthorization.TryValidate(
                request,
                oauthOptions.Value.ClientId,
                out var normalizedScope,
                out var error);
            return new OidcAuthorizationValidation
            {
                IsValid = valid,
                CanRedirect = false,
                NormalizedScope = normalizedScope,
                ClientName = valid ? "OAuth2" : null,
                Error = error
            };
        }

        if (string.IsNullOrWhiteSpace(request?.ClientId))
        {
            return Invalid("invalid_request");
        }

        var application = await applications.GetApplicationAsync(
            request.ClientId,
            cancellationToken);
        if (application is null)
        {
            return Invalid("invalid_client");
        }

        var isValid = OidcAuthorizationPolicy.TryValidateRegisteredApplication(
            request,
            application,
            out var scope,
            out var validationError,
            out var canRedirect);
        return new OidcAuthorizationValidation
        {
            IsValid = isValid,
            CanRedirect = canRedirect,
            NormalizedScope = scope,
            ClientName = isValid ? application.Application.Name : null,
            Error = validationError
        };
    }

    private static OidcAuthorizationValidation Invalid(string error) =>
        new()
        {
            IsValid = false,
            Error = error
        };
}
