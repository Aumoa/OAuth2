using OAuth2.Data;
using OAuth2.OpenId;

namespace OAuth2.Abstractions.Tests.OpenId;

public sealed class GroupClaimMappingPolicyTests
{
    private static readonly OrganizationClaimValue[] s_OrganizationClaims =
    [
        new()
        {
            Id = "project-ayla",
            Name = "Project Ayla",
            Role = OrganizationRoles.Member,
            GroupIds = ["admin", "developer"]
        },
        new()
        {
            Id = "child-project",
            Name = "Child project",
            Role = OrganizationRoles.Admin,
            GroupIds = ["operator"]
        }
    ];

    [Theory]
    [InlineData(GroupClaimFormats.Dash, "project-ayla", "project-ayla-admin")]
    [InlineData(GroupClaimFormats.Path, "/project-ayla", "/project-ayla/admin")]
    [InlineData(GroupClaimFormats.Colon, "project-ayla", "project-ayla:admin")]
    public void MapGroups_UsesSelectedFormat(
        string format,
        string organizationValue,
        string groupValue)
    {
        var mapping = new GroupClaimMapping
        {
            Format = format,
            Selectors = ["/project-ayla", "/project-ayla/admin"]
        };

        var result = GroupClaimMappingPolicy.MapGroups(s_OrganizationClaims, mapping);

        Assert.Equal(
            new[] { organizationValue, groupValue }.Order(StringComparer.Ordinal),
            result);
    }

    [Fact]
    public void MapGroups_WildcardIncludesAllGroupsButNotOrganization()
    {
        var mapping = new GroupClaimMapping
        {
            Format = GroupClaimFormats.Path,
            Selectors = ["/project-ayla/*"]
        };

        var result = GroupClaimMappingPolicy.MapGroups(s_OrganizationClaims, mapping);

        Assert.Equal(["/project-ayla/admin", "/project-ayla/developer"], result);
    }

    [Fact]
    public void MapGroups_ExactSelectorRequiresMembership()
    {
        var mapping = new GroupClaimMapping
        {
            Format = GroupClaimFormats.Colon,
            Selectors = ["/project-ayla/admin", "/project-ayla/owner"]
        };

        var result = GroupClaimMappingPolicy.MapGroups(s_OrganizationClaims, mapping);

        Assert.Equal(["project-ayla:admin"], result);
    }

    [Fact]
    public void FilterOrganizations_IncludesOnlyOrganizationsNamedBySelectors()
    {
        var mapping = new GroupClaimMapping
        {
            Format = GroupClaimFormats.Dash,
            Selectors = ["/child-project/*"]
        };

        var result = GroupClaimMappingPolicy.FilterOrganizations(
            s_OrganizationClaims,
            mapping);

        Assert.Equal(["child-project"], result.Select(static claim => claim.Id));
    }

    [Theory]
    [InlineData("/*")]
    [InlineData("/project-ayla/")]
    [InlineData("project-ayla/admin")]
    [InlineData("/Project-Ayla/admin")]
    [InlineData("/project-ayla/admin/member")]
    [InlineData("/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa/admin")]
    public void TryNormalize_RejectsInvalidSelector(string selector)
    {
        var mapping = new GroupClaimMapping
        {
            Format = GroupClaimFormats.Dash,
            Selectors = [selector]
        };

        Assert.False(GroupClaimMappingPolicy.TryNormalize(mapping, out _, out var error));
        Assert.Equal("group claim mapping contains an invalid selector", error);
    }

    [Fact]
    public void CreateDefault_UsesOnlyOwnerOrganizationGroups()
    {
        var mapping = GroupClaimMappingPolicy.CreateDefault("project-ayla");

        Assert.Equal(GroupClaimFormats.Dash, mapping.Format);
        Assert.Equal(["/project-ayla/*"], mapping.Selectors);
    }
}
