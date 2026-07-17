using OAuth2.OpenId;

namespace OAuth2.Abstractions.Tests.OpenId;

public sealed class OidcEndpointUrisTests
{
    [Theory]
    [InlineData("https://sso.example", "https://sso.example")]
    [InlineData("https://sso.example/", "https://sso.example")]
    [InlineData("http://localhost:5173", "http://localhost:5173")]
    public void TryNormalizeIssuer_AcceptsSecureOrLoopbackOrigins(
        string value,
        string expected)
    {
        Assert.True(OidcEndpointUris.TryNormalizeIssuer(value, out var issuer));
        Assert.Equal(expected, issuer);
    }

    [Theory]
    [InlineData("http://sso.example")]
    [InlineData("https://sso.example/path")]
    [InlineData("https://sso.example/?query=1")]
    public void TryNormalizeIssuer_RejectsUnsafeOrNonOriginValues(string value)
    {
        Assert.False(OidcEndpointUris.TryNormalizeIssuer(value, out _));
    }
}
