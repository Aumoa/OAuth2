using System.Diagnostics.CodeAnalysis;
using OAuth2.Data;

namespace OAuth2.OpenId;

public static class OidcAuthorizationPolicy
{
    public const string ConsentPrompt = "consent";

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
            || !configuration.RedirectUris.Any(registeredRedirectUri =>
                OidcRedirectUriPolicy.Matches(
                    request.RedirectUri,
                    registeredRedirectUri,
                    configuration.Application.ApplicationType)))
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

        var prompts = OidcScopePolicy.Split(request.Prompt);
        if (prompts.Distinct(StringComparer.Ordinal).Count() != prompts.Length
            || prompts.Any(prompt => !string.Equals(
                prompt,
                ConsentPrompt,
                StringComparison.Ordinal)))
        {
            error = "invalid_request";
            return false;
        }

        if (!OidcScopePolicy.TryNormalize(request.Scope, false, out normalizedScope))
        {
            error = "invalid_scope";
            return false;
        }

        var normalizedScopes = new HashSet<string>(
            OidcScopePolicy.Split(normalizedScope),
            StringComparer.Ordinal);
        if (normalizedScopes.Contains(OidcScopePolicy.OfflineAccessScope)
            && !prompts.Contains(ConsentPrompt, StringComparer.Ordinal))
        {
            normalizedScopes.Remove(OidcScopePolicy.OfflineAccessScope);
            if (!OidcScopePolicy.TryNormalize(
                    string.Join(' ', normalizedScopes),
                    false,
                    out normalizedScope))
            {
                error = "invalid_scope";
                return false;
            }
        }

        if (!OidcScopePolicy.Split(normalizedScope)
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
