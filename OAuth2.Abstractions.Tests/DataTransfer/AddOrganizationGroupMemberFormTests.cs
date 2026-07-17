using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class AddOrganizationGroupMemberFormTests
{
    [Fact]
    public void Verify_AcceptsAccountId()
    {
        var form = new AddOrganizationGroupMemberForm
        {
            AccountId = "member-account"
        };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Verify_RejectsMissingAccountId()
    {
        var form = new AddOrganizationGroupMemberForm
        {
            AccountId = " "
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.accountId is missing", error);
    }

    [Fact]
    public void Verify_RejectsAccountIdThatIsTooLong()
    {
        var form = new AddOrganizationGroupMemberForm
        {
            AccountId = new string('a', 129)
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.accountId exceeds 128 characters", error);
    }
}
