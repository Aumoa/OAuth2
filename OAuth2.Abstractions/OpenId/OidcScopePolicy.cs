using System.Diagnostics.CodeAnalysis;

namespace OAuth2.OpenId;

public static class OidcScopePolicy
{
    public const string AllScope = "all";

    public const string OpenIdScope = "openid";

    public const string OfflineAccessScope = "offline_access";

    public const string GroupsScope = "groups";

    public const string RolesScope = "roles";

    public const string OrganizationScope = "organization";

    public static readonly string[] DefaultApplicationScopes =
    [
        OpenIdScope,
        "profile",
        "email",
        "address",
        "phone",
        GroupsScope,
        RolesScope
    ];

    public static readonly string[] ClaimScopes =
    [
        .. DefaultApplicationScopes,
        OrganizationScope
    ];

    public static readonly string[] SupportedScopes =
    [
        .. ClaimScopes,
        OfflineAccessScope
    ];

    public static bool TryNormalize(
        string? scope,
        bool allowAll,
        [NotNullWhen(true)] out string? normalizedScope)
    {
        normalizedScope = null;
        var scopes = Split(scope);
        if (scopes.Length == 0)
        {
            return false;
        }

        var scopeSet = new HashSet<string>(scopes, StringComparer.Ordinal);
        if (scopeSet.Contains(AllScope))
        {
            if (!allowAll || scopeSet.Count != 1)
            {
                return false;
            }

            normalizedScope = AllScope;
            return true;
        }

        normalizedScope = JoinCanonical(scopeSet);
        return true;
    }

    public static bool TryCombine(
        string? grantedScope,
        string? sessionScope,
        [NotNullWhen(true)] out string? effectiveScope)
    {
        effectiveScope = null;
        if (!TryExpand(grantedScope, out var grantedScopes)
            || !TryExpand(sessionScope, out var sessionScopes)
            || !sessionScopes.IsSubsetOf(grantedScopes))
        {
            return false;
        }

        grantedScopes.UnionWith(sessionScopes);
        effectiveScope = JoinCanonical(grantedScopes);
        return true;
    }

    public static string[] Split(string? scope)
    {
        return (scope ?? string.Empty).Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    public static bool TryResolveRefreshScope(
        string? grantedScope,
        string? requestedScope,
        [NotNullWhen(true)] out string? accessTokenScope,
        [NotNullWhen(true)] out string? replacementGrantScope)
    {
        accessTokenScope = null;
        replacementGrantScope = null;
        if (!TryNormalize(grantedScope, false, out var normalizedGrant))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(requestedScope))
        {
            accessTokenScope = normalizedGrant;
            replacementGrantScope = normalizedGrant;
            return true;
        }

        if (!TryNormalize(requestedScope, false, out var normalizedRequest))
        {
            return false;
        }

        var requestedScopes = Split(normalizedRequest);
        if (!requestedScopes.Contains(OpenIdScope, StringComparer.Ordinal)
            || requestedScopes.Except(Split(normalizedGrant), StringComparer.Ordinal).Any())
        {
            return false;
        }

        var replacementScopes = new HashSet<string>(
            requestedScopes,
            StringComparer.Ordinal);
        if (Split(normalizedGrant).Contains(OfflineAccessScope, StringComparer.Ordinal))
        {
            replacementScopes.Add(OfflineAccessScope);
        }

        if (!TryNormalize(
                string.Join(' ', replacementScopes),
                false,
                out replacementGrantScope))
        {
            return false;
        }

        accessTokenScope = normalizedRequest;
        return true;
    }

    private static bool TryExpand(
        string? scope,
        [NotNullWhen(true)] out HashSet<string>? scopes)
    {
        scopes = null;
        if (!TryNormalize(scope, true, out var normalizedScope))
        {
            return false;
        }

        scopes = string.Equals(normalizedScope, AllScope, StringComparison.Ordinal)
            ? new HashSet<string>(ClaimScopes, StringComparer.Ordinal)
            : new HashSet<string>(Split(normalizedScope), StringComparer.Ordinal);
        return true;
    }

    private static string JoinCanonical(IEnumerable<string> scopes)
    {
        var scopeSet = new HashSet<string>(scopes, StringComparer.Ordinal);
        var knownScopes = SupportedScopes.Where(scopeSet.Remove);
        var extensionScopes = scopeSet.Order(StringComparer.Ordinal);
        return string.Join(' ', knownScopes.Concat(extensionScopes));
    }
}
