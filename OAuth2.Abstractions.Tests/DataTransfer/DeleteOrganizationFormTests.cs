using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class DeleteOrganizationFormTests
{
    [Fact]
    public void Verify_AcceptsOrganizationName()
    {
        var form = new DeleteOrganizationForm { Name = "Example Organization" };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Verify_RejectsMissingOrganizationName()
    {
        var form = new DeleteOrganizationForm { Name = " " };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.name is missing", error);
    }
}
