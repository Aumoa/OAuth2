using OAuth2.OpenId;

namespace OAuth2.Abstractions.Tests.OpenId;

public sealed class InternalOidcAuthorizationTests
{
    [Fact]
    public void TryValidate_AcceptsExactInternalRedirectAndNormalizesScope()
    {
        var request = CreateRequest() with
        {
            Scope = "email openid profile email"
        };

        var result = InternalOidcAuthorization.TryValidate(
            request,
            InternalOidcAuthorization.DefaultClientId,
            out var normalizedScope,
            out var error);

        Assert.True(result);
        Assert.Equal(InternalOidcAuthorization.Scope, normalizedScope);
        Assert.Null(error);
    }

    [Theory]
    [InlineData("https://localhost:7140/api/v1/auth/redirect")]
    [InlineData("//api/v1/auth/redirect")]
    [InlineData("/api/v1/auth/redirect/")]
    public void TryValidate_RejectsRedirectOtherThanExactRelativePath(string redirectUri)
    {
        var request = CreateRequest() with
        {
            RedirectUri = redirectUri
        };

        var result = InternalOidcAuthorization.TryValidate(
            request,
            InternalOidcAuthorization.DefaultClientId,
            out _,
            out var error);

        Assert.False(result);
        Assert.Equal("invalid_redirect_uri", error);
    }

    [Fact]
    public void TryValidate_RejectsMissingOpenIdScope()
    {
        var request = CreateRequest() with
        {
            Scope = "profile email"
        };

        var result = InternalOidcAuthorization.TryValidate(
            request,
            InternalOidcAuthorization.DefaultClientId,
            out _,
            out var error);

        Assert.False(result);
        Assert.Equal("invalid_scope", error);
    }

    [Fact]
    public void PkceValidate_AcceptsMatchingVerifierAndRejectsAnotherVerifier()
    {
        var verifier = Pkce.CreateCodeVerifier();
        var challenge = Pkce.CreateCodeChallenge(verifier);

        Assert.True(Pkce.Validate(verifier, challenge, "S256"));
        Assert.False(Pkce.Validate(Pkce.CreateCodeVerifier(), challenge, "S256"));
    }

    private static OidcAuthorizationRequest CreateRequest()
    {
        var verifier = Pkce.CreateCodeVerifier();
        return new OidcAuthorizationRequest
        {
            ClientId = InternalOidcAuthorization.DefaultClientId,
            RedirectUri = InternalOidcAuthorization.RedirectUri,
            ResponseType = "code",
            Scope = InternalOidcAuthorization.Scope,
            State = Pkce.CreateCodeVerifier(),
            Nonce = Pkce.CreateCodeVerifier(),
            CodeChallenge = Pkce.CreateCodeChallenge(verifier),
            CodeChallengeMethod = "S256"
        };
    }
}
