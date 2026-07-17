using System.Diagnostics.CodeAnalysis;
using OAuth2.OpenId;

namespace OAuth2.DataTransfer;

public sealed record UpdateApplicationForm
{
    public const int MaxRedirectUris = 20;

    public const int RedirectUriMaxLength = 2048;

    public required IReadOnlyList<string> RedirectUris { get; init; }

    public required IReadOnlyList<string> AllowedScopes { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (RedirectUris is null)
        {
            error = "body.redirectUris is missing";
            return false;
        }

        if (RedirectUris.Count > MaxRedirectUris)
        {
            error = $"body.redirectUris exceeds {MaxRedirectUris} entries";
            return false;
        }

        var redirectUris = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in RedirectUris)
        {
            var redirectUri = value?.Trim();
            if (string.IsNullOrWhiteSpace(redirectUri))
            {
                error = "body.redirectUris contains an empty value";
                return false;
            }

            if (redirectUri.Length > RedirectUriMaxLength)
            {
                error = $"body.redirectUris contains a value exceeding {RedirectUriMaxLength} characters";
                return false;
            }

            if (!OidcRedirectUriPolicy.IsValidForAnyApplicationType(redirectUri))
            {
                error = "body.redirectUris contains an invalid redirect URI";
                return false;
            }

            if (!redirectUris.Add(redirectUri))
            {
                error = "body.redirectUris contains a duplicate value";
                return false;
            }
        }

        if (AllowedScopes is null)
        {
            error = "body.allowedScopes is missing";
            return false;
        }

        var allowedScopes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in AllowedScopes)
        {
            var scope = value?.Trim();
            if (string.IsNullOrWhiteSpace(scope)
                || !OidcScopePolicy.ClaimScopes.Contains(scope, StringComparer.Ordinal))
            {
                error = "body.allowedScopes contains an unsupported scope";
                return false;
            }

            if (!allowedScopes.Add(scope))
            {
                error = "body.allowedScopes contains a duplicate value";
                return false;
            }
        }

        if (!allowedScopes.Contains(OidcScopePolicy.OpenIdScope))
        {
            error = $"body.allowedScopes must contain {OidcScopePolicy.OpenIdScope}";
            return false;
        }

        error = null;
        return true;
    }
}
