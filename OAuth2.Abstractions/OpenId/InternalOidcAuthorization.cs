using System.Diagnostics.CodeAnalysis;

namespace OAuth2.OpenId;

public static class InternalOidcAuthorization
{
    public const string DefaultClientId = "oauth2";
    public const string RedirectUri = "/api/v1/auth/redirect";
    public const string Scope = "openid profile email";

    private static readonly string[] s_AllowedScopes = ["openid", "profile", "email"];

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

        if (!TryNormalizeScope(request.Scope, out normalizedScope))
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

    private static bool TryNormalizeScope(
        string? scope,
        [NotNullWhen(true)] out string? normalizedScope)
    {
        normalizedScope = null;
        if (string.IsNullOrWhiteSpace(scope))
        {
            return false;
        }

        var requestedScopes = scope.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var requestedSet = new HashSet<string>(requestedScopes, StringComparer.Ordinal);
        if (!requestedSet.Contains("openid")
            || requestedSet.Any(value => !s_AllowedScopes.Contains(value, StringComparer.Ordinal)))
        {
            return false;
        }

        normalizedScope = string.Join(' ', s_AllowedScopes.Where(requestedSet.Contains));
        return true;
    }
}
