using System.Diagnostics.CodeAnalysis;
using OAuth2.Data;

namespace OAuth2.OpenId;

public static class OidcAuthorizationPolicy
{
    public static bool TryValidateRegisteredApplication(
        OidcAuthorizationRequest? request,
        OAuthApplicationConfiguration configuration,
        [NotNullWhen(true)] out string? normalizedScope,
        [NotNullWhen(false)] out string? error,
        out bool canRedirect)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        normalizedScope = null;
        canRedirect = false;

        if (request is null)
        {
            error = "invalid_request";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.ClientId)
            || !string.Equals(
                request.ClientId,
                configuration.Application.Id,
                StringComparison.Ordinal))
        {
            error = "invalid_client";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.RedirectUri)
            || !configuration.RedirectUris.Contains(request.RedirectUri, StringComparer.Ordinal))
        {
            error = "invalid_redirect_uri";
            return false;
        }

        canRedirect = true;

        if (!string.Equals(request.ResponseType, "code", StringComparison.Ordinal))
        {
            error = "unsupported_response_type";
            return false;
        }

        if (!OidcScopePolicy.TryNormalize(request.Scope, false, out normalizedScope)
            || !OidcScopePolicy.Split(normalizedScope)
                .Contains(OidcScopePolicy.OpenIdScope, StringComparer.Ordinal)
            || OidcScopePolicy.Split(normalizedScope)
                .Except(configuration.AllowedScopes, StringComparer.Ordinal)
                .Any())
        {
            normalizedScope = null;
            error = "invalid_scope";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.State))
        {
            normalizedScope = null;
            error = "invalid_request";
            return false;
        }

        if (!Pkce.IsValidChallenge(request.CodeChallenge, request.CodeChallengeMethod))
        {
            normalizedScope = null;
            error = "invalid_request";
            return false;
        }

        error = null;
        return true;
    }
}
