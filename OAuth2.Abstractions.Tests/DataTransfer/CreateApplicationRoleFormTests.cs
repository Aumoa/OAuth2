using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class CreateApplicationRoleFormTests
{
    [Theory]
    [InlineData("admin")]
    [InlineData("content-editor")]
    [InlineData("billing.read")]
    [InlineData("project_admin")]
    [InlineData("resource:write")]
    public void Verify_AcceptsSupportedRoleIds(string roleId)
    {
        var form = new CreateApplicationRoleForm
        {
            Id = roleId,
            Name = "Example role"
        };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("admin role")]
    [InlineData("admin--role")]
    [InlineData("-admin")]
    [InlineData("admin-")]
    public void Verify_RejectsUnsupportedRoleIds(string roleId)
    {
        var form = new CreateApplicationRoleForm
        {
            Id = roleId,
            Name = "Example role"
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.id contains unsupported characters", error);
    }

    [Fact]
    public void Verify_RejectsMissingName()
    {
        var form = new CreateApplicationRoleForm
        {
            Id = "admin",
            Name = ""
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.name is missing", error);
    }
}
