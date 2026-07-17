using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class AddOrganizationMemberFormTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("member")]
    public void Verify_AcceptsAssignableRoles(string role)
    {
        var form = new AddOrganizationMemberForm
        {
            AccountId = "member-account",
            Role = role
        };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("unknown")]
    public void Verify_RejectsUnassignableRoles(string role)
    {
        var form = new AddOrganizationMemberForm
        {
            AccountId = "member-account",
            Role = role
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.role must be admin or member", error);
    }
}
