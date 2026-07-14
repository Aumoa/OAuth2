using System.Text.Json;
using OAuth2.OpenId;

namespace OAuth2.Abstractions.Tests.OpenId;

public sealed class OidcScopePolicyTests
{
    [Fact]
    public void TryCombine_ReturnsUnionWhenSessionScopeIsWithinGrantedScope()
    {
        var result = OidcScopePolicy.TryCombine(
            "openid profile email",
            "openid profile",
            out var effectiveScope);

        Assert.True(result);
        Assert.Equal("openid profile email", effectiveScope);
    }

    [Fact]
    public void TryCombine_RejectsSessionScopeBroaderThanGrantedScope()
    {
        var result = OidcScopePolicy.TryCombine(
            "openid profile",
            "openid profile email",
            out var effectiveScope);

        Assert.False(result);
        Assert.Null(effectiveScope);
    }

    [Fact]
    public void TryCombine_ExpandsAllToEveryClaimScope()
    {
        var result = OidcScopePolicy.TryCombine("all", "all", out var effectiveScope);

        Assert.True(result);
        Assert.Equal(string.Join(' ', OidcScopePolicy.ClaimScopes), effectiveScope);
    }

    [Fact]
    public void Filter_ReturnsOnlyClaimsAllowedByEffectiveScope()
    {
        var claims = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["sub"] = JsonSerializer.SerializeToElement("subject"),
            ["name"] = JsonSerializer.SerializeToElement("User"),
            ["email"] = JsonSerializer.SerializeToElement("user@example.com")
        };

        var filtered = OidcClaimPolicy.Filter(claims, "openid profile");

        Assert.Equal(2, filtered.Count);
        Assert.Contains("sub", filtered);
        Assert.Contains("name", filtered);
        Assert.DoesNotContain("email", filtered);
    }
}
