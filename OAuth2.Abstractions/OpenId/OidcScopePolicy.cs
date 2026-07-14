using System.Diagnostics.CodeAnalysis;

namespace OAuth2.OpenId;

public static class OidcScopePolicy
{
    public const string AllScope = "all";

    public static readonly string[] ClaimScopes =
    [
        "openid",
        "profile",
        "email",
        "address",
        "phone",
        "groups"
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
        var knownScopes = ClaimScopes.Where(scopeSet.Remove);
        var extensionScopes = scopeSet.Order(StringComparer.Ordinal);
        return string.Join(' ', knownScopes.Concat(extensionScopes));
    }
}
