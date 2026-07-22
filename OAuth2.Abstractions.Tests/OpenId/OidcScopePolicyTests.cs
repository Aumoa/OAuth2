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
        Assert.DoesNotContain(OidcScopePolicy.OfflineAccessScope, effectiveScope);
    }

    [Fact]
    public void TryNormalize_PutsOfflineAccessAfterClaimScopes()
    {
        var result = OidcScopePolicy.TryNormalize(
            "offline_access email openid",
            false,
            out var normalizedScope);

        Assert.True(result);
        Assert.Equal("openid email offline_access", normalizedScope);
        Assert.Contains(
            OidcScopePolicy.OfflineAccessScope,
            OidcScopePolicy.SupportedScopes);
    }

    [Fact]
    public void OrganizationScope_IsSupportedButRequiresApplicationOptIn()
    {
        Assert.Contains(
            OidcScopePolicy.OrganizationScope,
            OidcScopePolicy.SupportedScopes);
        Assert.DoesNotContain(
            OidcScopePolicy.OrganizationScope,
            OidcScopePolicy.DefaultApplicationScopes);
    }

    [Fact]
    public void RolesScope_IsSupportedForApplications()
    {
        Assert.Contains(OidcScopePolicy.RolesScope, OidcScopePolicy.SupportedScopes);
        Assert.Contains(OidcScopePolicy.RolesScope, OidcScopePolicy.DefaultApplicationScopes);
    }

    [Fact]
    public void TryResolveRefreshScope_PreservesOfflineGrantWhenAccessScopeIsNarrowed()
    {
        var result = OidcScopePolicy.TryResolveRefreshScope(
            "openid profile offline_access",
            "openid",
            out var accessTokenScope,
            out var replacementGrantScope);

        Assert.True(result);
        Assert.Equal("openid", accessTokenScope);
        Assert.Equal("openid offline_access", replacementGrantScope);
    }

    [Fact]
    public void TryResolveRefreshScope_RejectsScopeOutsideOriginalGrant()
    {
        var result = OidcScopePolicy.TryResolveRefreshScope(
            "openid offline_access",
            "openid email",
            out var accessTokenScope,
            out var replacementGrantScope);

        Assert.False(result);
        Assert.Null(accessTokenScope);
        Assert.Null(replacementGrantScope);
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

    [Fact]
    public void Filter_SeparatesGroupsAndOrganizationClaims()
    {
        var claims = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["sub"] = JsonSerializer.SerializeToElement("subject"),
            ["groups"] = JsonSerializer.SerializeToElement(new[] { "example" }),
            ["organization"] = JsonSerializer.SerializeToElement(new[]
            {
                new { id = "example", name = "Example", role = "member" }
            })
        };

        var groupsOnly = OidcClaimPolicy.Filter(claims, "openid groups");
        var organizationOnly = OidcClaimPolicy.Filter(claims, "openid organization");

        Assert.Contains("groups", groupsOnly);
        Assert.DoesNotContain("organization", groupsOnly);
        Assert.Contains("organization", organizationOnly);
        Assert.DoesNotContain("groups", organizationOnly);
    }

    [Fact]
    public void Filter_ReturnsRolesOnlyForRolesScope()
    {
        var claims = new Dictionary<string, JsonElement>(StringComparer.Ordinal)
        {
            ["sub"] = JsonSerializer.SerializeToElement("subject"),
            ["roles"] = JsonSerializer.SerializeToElement(new[] { "admin" })
        };

        var withoutRoles = OidcClaimPolicy.Filter(claims, "openid");
        var withRoles = OidcClaimPolicy.Filter(claims, "openid roles");

        Assert.DoesNotContain("roles", withoutRoles);
        Assert.Contains("roles", withRoles);
    }
}
