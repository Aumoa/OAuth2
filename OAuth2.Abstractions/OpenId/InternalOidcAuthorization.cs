using System.Diagnostics.CodeAnalysis;

namespace OAuth2.OpenId;

public static class InternalOidcAuthorization
{
    public const string DefaultClientId = "oauth2";
    public const string RedirectUri = "/";
    public const string CallbackPath = "/api/v1/auth/callback";
    public const string Scope = OidcScopePolicy.AllScope;

    public static bool TryValidate(
        OidcAuthorizationRequest? request,
        string clientId,
        [NotNullWhen(true)] out string? normalizedScope,
        [NotNullWhen(false)] out string? error)
    {
        normalizedScope = null;

        if (request is null)
        {
            error = "authorization request is missing";
            return false;
        }

        if (!string.Equals(request.ClientId, clientId, StringComparison.Ordinal))
        {
            error = "invalid_client";
            return false;
        }

        if (!string.Equals(request.RedirectUri, RedirectUri, StringComparison.Ordinal))
        {
            error = "invalid_redirect_uri";
            return false;
        }

        if (!string.Equals(request.ResponseType, "code", StringComparison.Ordinal))
        {
            error = "unsupported_response_type";
            return false;
        }

        if (!OidcScopePolicy.TryNormalize(request.Scope, true, out normalizedScope)
            || !string.Equals(normalizedScope, Scope, StringComparison.Ordinal))
        {
            error = "invalid_scope";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.State))
        {
            error = "state is required";
            return false;
        }

        if (!Pkce.IsValidChallenge(request.CodeChallenge, request.CodeChallengeMethod))
        {
            error = "PKCE S256 code challenge is required";
            return false;
        }

        error = null;
        return true;
    }

}
