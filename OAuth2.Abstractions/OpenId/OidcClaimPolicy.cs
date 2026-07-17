using System.Text.Json;

namespace OAuth2.OpenId;

public static class OidcClaimPolicy
{
    private static readonly IReadOnlyDictionary<string, string[]> s_ClaimsByScope =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["openid"] = ["sub"],
            ["profile"] =
            [
                "name",
                "family_name",
                "given_name",
                "middle_name",
                "nickname",
                "preferred_username",
                "profile",
                "picture",
                "website",
                "gender",
                "birthdate",
                "zoneinfo",
                "locale",
                "updated_at"
            ],
            ["email"] = ["email", "email_verified"],
            ["address"] = ["address"],
            ["phone"] = ["phone_number", "phone_number_verified"],
            ["groups"] = ["groups"],
            ["organization"] = ["organization"]
        };

    public static IReadOnlyList<string> ClaimNames { get; } =
        s_ClaimsByScope.Values
            .SelectMany(static claims => claims)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    public static HashSet<string> GetAllowedClaimNames(string scope)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        var normalizedScope = string.Equals(scope, OidcScopePolicy.AllScope, StringComparison.Ordinal)
            ? string.Join(' ', OidcScopePolicy.ClaimScopes)
            : scope;
        var claimNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var currentScope in OidcScopePolicy.Split(normalizedScope))
        {
            if (s_ClaimsByScope.TryGetValue(currentScope, out var mappedClaims))
            {
                claimNames.UnionWith(mappedClaims);
            }
        }

        return claimNames;
    }

    public static Dictionary<string, JsonElement> Filter(
        IReadOnlyDictionary<string, JsonElement> claims,
        string scope)
    {
        ArgumentNullException.ThrowIfNull(claims);

        var allowedClaimNames = GetAllowedClaimNames(scope);
        return claims
            .Where(pair => allowedClaimNames.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
    }
}
