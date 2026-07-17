using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class TransferOrganizationOwnershipFormTests
{
    [Fact]
    public void Verify_AcceptsAccountId()
    {
        var form = new TransferOrganizationOwnershipForm
        {
            AccountId = "next-owner"
        };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Verify_RejectsMissingAccountId()
    {
        var form = new TransferOrganizationOwnershipForm
        {
            AccountId = " "
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.accountId is missing", error);
    }
}
