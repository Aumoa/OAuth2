using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class AddOrganizationMemberFormTests
{
    [Theory]
    [InlineData("member-account", "admin")]
    [InlineData("member@example.com", "member")]
    public void Verify_AcceptsAccountIdOrEmailWithAssignableRole(string accountIdentifier, string role)
    {
        var form = new AddOrganizationMemberForm
        {
            AccountId = accountIdentifier,
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
