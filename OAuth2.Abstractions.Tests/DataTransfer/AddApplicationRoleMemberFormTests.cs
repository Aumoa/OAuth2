using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class AddApplicationRoleMemberFormTests
{
    [Theory]
    [InlineData("account-id")]
    [InlineData("user@example.com")]
    public void Verify_AcceptsAccountIdOrEmail(string accountId)
    {
        var form = new AddApplicationRoleMemberForm { AccountId = accountId };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Verify_RejectsMissingAccountIdentifier()
    {
        var form = new AddApplicationRoleMemberForm { AccountId = " " };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.accountId is missing", error);
    }
}
