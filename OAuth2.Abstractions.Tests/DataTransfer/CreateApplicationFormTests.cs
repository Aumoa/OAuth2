using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class CreateApplicationFormTests
{
    [Fact]
    public void Verify_AcceptsClientIdAndName()
    {
        var form = new CreateApplicationForm
        {
            ClientId = "example-client",
            Name = "Example application"
        };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData("", "Example application", "body.clientId is missing")]
    [InlineData("example-client", "", "body.name is missing")]
    public void Verify_RejectsMissingValues(string clientId, string name, string expectedError)
    {
        var form = new CreateApplicationForm
        {
            ClientId = clientId,
            Name = name
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal(expectedError, error);
    }

    [Fact]
    public void Verify_RejectsClientIdThatIsTooLong()
    {
        var form = new CreateApplicationForm
        {
            ClientId = new string('a', CreateApplicationForm.ClientIdMaxLength + 1),
            Name = "Example application"
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.clientId exceeds 128 characters", error);
    }

    [Fact]
    public void Verify_RejectsNameThatIsTooLong()
    {
        var form = new CreateApplicationForm
        {
            ClientId = "example-client",
            Name = new string('a', CreateApplicationForm.NameMaxLength + 1)
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.name exceeds 512 characters", error);
    }
}
