using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class CreateOrganizationFormTests
{
    [Fact]
    public void Verify_AcceptsUrlSafeIdAndName()
    {
        var form = new CreateOrganizationForm
        {
            Id = "example-team",
            Name = "Example team"
        };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData("", "Example team", "body.id is missing")]
    [InlineData("Example-Team", "Example team", "body.id must contain only lowercase letters, numbers, or single hyphens")]
    [InlineData("example--team", "Example team", "body.id must contain only lowercase letters, numbers, or single hyphens")]
    [InlineData("new", "Example team", "body.id is reserved")]
    [InlineData("example-team", "", "body.name is missing")]
    public void Verify_RejectsInvalidValues(string id, string name, string expectedError)
    {
        var form = new CreateOrganizationForm
        {
            Id = id,
            Name = name
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal(expectedError, error);
    }

    [Fact]
    public void Verify_RejectsIdThatIsTooLong()
    {
        var form = new CreateOrganizationForm
        {
            Id = new string('a', CreateOrganizationForm.IdMaxLength + 1),
            Name = "Example team"
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.id exceeds 64 characters", error);
    }

    [Fact]
    public void Verify_RejectsNameThatIsTooLong()
    {
        var form = new CreateOrganizationForm
        {
            Id = "example-team",
            Name = new string('a', CreateOrganizationForm.NameMaxLength + 1)
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.name exceeds 128 characters", error);
    }
}
