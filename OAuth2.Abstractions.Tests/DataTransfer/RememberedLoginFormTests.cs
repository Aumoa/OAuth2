using OAuth2.DataTransfer;
using OAuth2.OpenId;

namespace OAuth2.Abstractions.Tests.DataTransfer;

public sealed class RememberedLoginFormTests
{
    [Fact]
    public void Verify_AcceptsTokenAndAuthorization()
    {
        var form = new RememberedLoginForm
        {
            Token = Pkce.CreateCodeVerifier(),
            Authorization = new OidcAuthorizationRequest()
        };

        Assert.True(form.Verify(out var error));
        Assert.Null(error);
    }

    [Fact]
    public void Verify_RejectsMissingToken()
    {
        var form = new RememberedLoginForm
        {
            Token = string.Empty,
            Authorization = new OidcAuthorizationRequest()
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.token is missing", error);
    }

    [Fact]
    public void Verify_RejectsMissingAuthorization()
    {
        var form = new RememberedLoginForm
        {
            Token = Pkce.CreateCodeVerifier()
        };

        Assert.False(form.Verify(out var error));
        Assert.Equal("body.authorization is missing", error);
    }
}
