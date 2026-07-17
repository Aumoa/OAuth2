using OAuth2.Data;
using OAuth2.OpenId;

namespace OAuth2.Abstractions.Tests.OpenId;

public sealed class OidcAuthorizationPolicyTests
{
    [Fact]
    public void TryValidateRegisteredApplication_AcceptsExactRedirectScopeAndPkce()
    {
        var result = OidcAuthorizationPolicy.TryValidateRegisteredApplication(
            CreateRequest(),
            CreateApplication(),
            out var scope,
            out var error,
            out var canRedirect);

        Assert.True(result);
        Assert.True(canRedirect);
        Assert.Equal("openid profile email", scope);
        Assert.Null(error);
    }

    [Fact]
    public void TryValidateRegisteredApplication_RejectsUnregisteredRedirectWithoutRedirecting()
    {
        var request = CreateRequest() with
        {
            RedirectUri = "https://attacker.example/callback"
        };

        var result = OidcAuthorizationPolicy.TryValidateRegisteredApplication(
            request,
            CreateApplication(),
            out _,
            out var error,
            out var canRedirect);

        Assert.False(result);
        Assert.False(canRedirect);
        Assert.Equal("invalid_redirect_uri", error);
    }

    [Theory]
    [InlineData("openid groups")]
    [InlineData("profile email")]
    [InlineData("all")]
    public void TryValidateRegisteredApplication_RejectsDisallowedOrNonOidcScope(string scope)
    {
        var result = OidcAuthorizationPolicy.TryValidateRegisteredApplication(
            CreateRequest() with { Scope = scope },
            CreateApplication(),
            out _,
            out var error,
            out var canRedirect);

        Assert.False(result);
        Assert.True(canRedirect);
        Assert.Equal("invalid_scope", error);
    }

    [Fact]
    public void TryValidateRegisteredApplication_RequiresS256Pkce()
    {
        var result = OidcAuthorizationPolicy.TryValidateRegisteredApplication(
            CreateRequest() with { CodeChallengeMethod = "plain" },
            CreateApplication(),
            out _,
            out var error,
            out var canRedirect);

        Assert.False(result);
        Assert.True(canRedirect);
        Assert.Equal("invalid_request", error);
    }

    [Fact]
    public void TryValidateRegisteredApplication_AcceptsDynamicMacOsLoopbackPort()
    {
        var request = CreateRequest() with
        {
            RedirectUri = "http://127.0.0.1:52147/signin-oidc"
        };
        var application = CreateApplication(
            OAuthApplicationTypes.MacOs,
            "http://127.0.0.1/signin-oidc");

        var result = OidcAuthorizationPolicy.TryValidateRegisteredApplication(
            request,
            application,
            out _,
            out var error,
            out var canRedirect);

        Assert.True(result);
        Assert.True(canRedirect);
        Assert.Null(error);
    }

    [Fact]
    public void TryValidateRegisteredApplication_IgnoresOfflineAccessWithoutConsentPrompt()
    {
        var request = CreateRequest() with
        {
            Scope = "openid profile offline_access"
        };
        var application = CreateApplication() with
        {
            AllowedScopes = ["openid", "profile", "email", "offline_access"]
        };

        var result = OidcAuthorizationPolicy.TryValidateRegisteredApplication(
            request,
            application,
            out var scope,
            out var error,
            out _);

        Assert.True(result);
        Assert.Equal("openid profile", scope);
        Assert.Null(error);
    }

    [Fact]
    public void TryValidateRegisteredApplication_AcceptsOfflineAccessWithConsentPrompt()
    {
        var request = CreateRequest() with
        {
            Scope = "offline_access openid profile",
            Prompt = "consent"
        };
        var application = CreateApplication() with
        {
            AllowedScopes = ["openid", "profile", "email", "offline_access"]
        };

        var result = OidcAuthorizationPolicy.TryValidateRegisteredApplication(
            request,
            application,
            out var scope,
            out var error,
            out _);

        Assert.True(result);
        Assert.Equal("openid profile offline_access", scope);
        Assert.Null(error);
    }

    [Fact]
    public void TryValidateRegisteredApplication_RejectsUnsupportedPrompt()
    {
        var result = OidcAuthorizationPolicy.TryValidateRegisteredApplication(
            CreateRequest() with { Prompt = "none consent" },
            CreateApplication(),
            out _,
            out var error,
            out var canRedirect);

        Assert.False(result);
        Assert.True(canRedirect);
        Assert.Equal("invalid_request", error);
    }

    private static OidcAuthorizationRequest CreateRequest()
    {
        var verifier = Pkce.CreateCodeVerifier();
        return new OidcAuthorizationRequest
        {
            ClientId = "service-client",
            RedirectUri = "https://service.example/signin-oidc",
            ResponseType = "code",
            Scope = "email openid profile",
            State = Pkce.CreateCodeVerifier(),
            Nonce = Pkce.CreateCodeVerifier(),
            CodeChallenge = Pkce.CreateCodeChallenge(verifier),
            CodeChallengeMethod = "S256"
        };
    }

    private static OAuthApplicationConfiguration CreateApplication(
        string applicationType = OAuthApplicationTypes.Web,
        string redirectUri = "https://service.example/signin-oidc") =>
        new()
        {
            Application = new OAuthApplication
            {
                Id = "service-client",
                OwnerId = "owner",
                Name = "Service",
                ApplicationType = applicationType,
                CreatedAt = DateTime.UtcNow
            },
            RedirectUris = [redirectUri],
            AllowedScopes = ["openid", "profile", "email"]
        };
}
