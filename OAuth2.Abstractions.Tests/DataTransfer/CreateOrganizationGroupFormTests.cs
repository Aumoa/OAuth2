using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class CreateOrganizationGroupFormTests
{
    [Fact]
    public void Verify_AcceptsUrlSafeIdAndName()
    {
        var form = new CreateOrganizationGroupForm
        {
            Id = "platform-team",
            Name = "Platform team"
        };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData("", "Platform team", "body.id is missing")]
    [InlineData("Platform-Team", "Platform team", "body.id must contain only lowercase letters, numbers, or single hyphens")]
    [InlineData("platform--team", "Platform team", "body.id must contain only lowercase letters, numbers, or single hyphens")]
    [InlineData("platform-team", "", "body.name is missing")]
    public void Verify_RejectsInvalidValues(string id, string name, string expectedError)
    {
        var form = new CreateOrganizationGroupForm
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
        var form = new CreateOrganizationGroupForm
        {
            Id = new string('a', CreateOrganizationGroupForm.IdMaxLength + 1),
            Name = "Platform team"
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.id exceeds 64 characters", error);
    }

    [Fact]
    public void Verify_RejectsNameThatIsTooLong()
    {
        var form = new CreateOrganizationGroupForm
        {
            Id = "platform-team",
            Name = new string('a', CreateOrganizationGroupForm.NameMaxLength + 1)
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.name exceeds 128 characters", error);
    }
}
