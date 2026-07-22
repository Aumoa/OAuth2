using OAuth2.DataTransfer;

namespace OAuth2.Tests.DataTransfer;

public sealed class UpdateAccountProfileFormTests
{
    [Fact]
    public void Verify_AcceptsEditableProfileClaims()
    {
        var form = new UpdateAccountProfileForm
        {
            FullName = "Ada Lovelace",
            Nickname = "Ada",
            Claims =
            [
                new AccountProfileClaim { Name = "family_name", Value = "Lovelace" },
                new AccountProfileClaim { Name = "given_name", Value = "Ada" },
                new AccountProfileClaim { Name = "locale", Value = "en-GB" }
            ]
        };

        Assert.True(form.Verify(out var error), error);
    }

    [Fact]
    public void Verify_AllowsNicknameRemoval()
    {
        var form = new UpdateAccountProfileForm
        {
            FullName = "Ada Lovelace",
            Nickname = " ",
            Claims = []
        };

        Assert.True(form.Verify(out var error), error);
    }

    [Theory]
    [InlineData("nickname")]
    [InlineData("email")]
    [InlineData("groups")]
    [InlineData("unknown")]
    public void Verify_RejectsClaimsThatAreNotAdditionalProfileFields(string name)
    {
        var form = new UpdateAccountProfileForm
        {
            FullName = "Ada Lovelace",
            Claims = [new AccountProfileClaim { Name = name, Value = "value" }]
        };

        Assert.False(form.Verify(out _));
    }

    [Fact]
    public void Verify_RejectsDuplicateClaimTypes()
    {
        var form = new UpdateAccountProfileForm
        {
            FullName = "Ada Lovelace",
            Claims =
            [
                new AccountProfileClaim { Name = "given_name", Value = "Ada" },
                new AccountProfileClaim { Name = "given_name", Value = "Augusta" }
            ]
        };

        Assert.False(form.Verify(out _));
    }
}
