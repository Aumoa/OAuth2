using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class UpdateOrganizationMemberFormTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("member")]
    public void Verify_AcceptsAssignableRoles(string role)
    {
        var form = new UpdateOrganizationMemberForm
        {
            Role = role
        };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Verify_RequiresDedicatedOwnershipTransferForOwnerRole()
    {
        var form = new UpdateOrganizationMemberForm
        {
            Role = "owner"
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.role must be admin or member", error);
    }
}
