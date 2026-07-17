using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class AddOrganizationGroupMemberFormTests
{
    [Theory]
    [InlineData("member-account")]
    [InlineData("member@example.com")]
    public void Verify_AcceptsAccountIdOrEmail(string accountIdentifier)
    {
        var form = new AddOrganizationGroupMemberForm
        {
            AccountId = accountIdentifier
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
