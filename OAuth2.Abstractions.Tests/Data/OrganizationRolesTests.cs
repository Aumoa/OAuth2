using OAuth2.Data;

namespace OAuth2.Abstractions.Tests.Data;

public sealed class OrganizationRolesTests
{
    [Theory]
    [InlineData(OrganizationRoles.Owner, OrganizationRoles.Admin, true)]
    [InlineData(OrganizationRoles.Owner, OrganizationRoles.Member, true)]
    [InlineData(OrganizationRoles.Admin, OrganizationRoles.Member, true)]
    [InlineData(OrganizationRoles.Admin, OrganizationRoles.Admin, false)]
    [InlineData(OrganizationRoles.Member, OrganizationRoles.Member, false)]
    [InlineData(OrganizationRoles.Member, OrganizationRoles.Owner, false)]
    public void CanManage_AllowsOnlyStrictlyLowerRoles(
        string actorRole,
        string targetRole,
        bool expected)
    {
        Assert.Equal(expected, OrganizationRoles.CanManage(actorRole, targetRole));
    }

    [Theory]
    [InlineData(OrganizationRoles.Owner, OrganizationRoles.Admin, true)]
    [InlineData(OrganizationRoles.Owner, OrganizationRoles.Member, true)]
    [InlineData(OrganizationRoles.Admin, OrganizationRoles.Member, true)]
    [InlineData(OrganizationRoles.Admin, OrganizationRoles.Admin, false)]
    [InlineData(OrganizationRoles.Member, OrganizationRoles.Member, false)]
    public void CanAdd_AllowsOnlyLowerAssignableRoles(
        string actorRole,
        string role,
        bool expected)
    {
        Assert.Equal(expected, OrganizationRoles.CanAdd(actorRole, role));
    }

    [Theory]
    [InlineData(OrganizationRoles.Owner, OrganizationRoles.Admin, true)]
    [InlineData(OrganizationRoles.Owner, OrganizationRoles.Member, true)]
    [InlineData(OrganizationRoles.Admin, OrganizationRoles.Admin, true)]
    [InlineData(OrganizationRoles.Admin, OrganizationRoles.Member, true)]
    [InlineData(OrganizationRoles.Member, OrganizationRoles.Member, true)]
    [InlineData(OrganizationRoles.Member, OrganizationRoles.Admin, false)]
    [InlineData(OrganizationRoles.Owner, OrganizationRoles.Owner, false)]
    public void CanAssign_AllowsAssignableRolesAtOrBelowActor(
        string actorRole,
        string role,
        bool expected)
    {
        Assert.Equal(expected, OrganizationRoles.CanAssign(actorRole, role));
    }
}
