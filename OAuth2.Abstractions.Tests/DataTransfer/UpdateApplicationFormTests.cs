using OAuth2.DataTransfer;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class UpdateApplicationFormTests
{
    [Fact]
    public void Verify_AcceptsSecureAndLoopbackRedirectUris()
    {
        var form = new UpdateApplicationForm
        {
            RedirectUris =
            [
                "https://example.com/oauth/callback",
                "http://localhost:5173/callback"
            ],
            AllowedScopes = ["openid", "profile", "email"]
        };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Verify_AcceptsEmptyConfiguration()
    {
        var form = new UpdateApplicationForm
        {
            RedirectUris = [],
            AllowedScopes = []
        };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Theory]
    [InlineData("http://example.com/callback")]
    [InlineData("https://user@example.com/callback")]
    [InlineData("https://example.com/callback#fragment")]
    [InlineData("not-a-uri")]
    public void Verify_RejectsInvalidRedirectUri(string redirectUri)
    {
        var form = new UpdateApplicationForm
        {
            RedirectUris = [redirectUri],
            AllowedScopes = ["openid"]
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.redirectUris contains an invalid redirect URI", error);
    }

    [Fact]
    public void Verify_RejectsDuplicateRedirectUri()
    {
        var form = new UpdateApplicationForm
        {
            RedirectUris = ["https://example.com/callback", "https://example.com/callback"],
            AllowedScopes = ["openid"]
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.redirectUris contains a duplicate value", error);
    }

    [Fact]
    public void Verify_RejectsUnsupportedScope()
    {
        var form = new UpdateApplicationForm
        {
            RedirectUris = [],
            AllowedScopes = ["administrator"]
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.allowedScopes contains an unsupported scope", error);
    }

    [Fact]
    public void Verify_RejectsDuplicateScope()
    {
        var form = new UpdateApplicationForm
        {
            RedirectUris = [],
            AllowedScopes = ["openid", "openid"]
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.allowedScopes contains a duplicate value", error);
    }
}
