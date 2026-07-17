using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class RegisterFormTests
{
    [Fact]
    public void Verify_RejectsOrganizationOwnerNamespace()
    {
        var form = new RegisterForm
        {
            Id = "organization:internal-owner",
            Password = "password",
            FullName = "Example User",
            Email = "user@example.com"
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.id is reserved", error);
    }
}
