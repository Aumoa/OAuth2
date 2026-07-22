using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OAuth2.Data;
using OAuth2.OpenId;
using OAuth2.Services;

namespace OAuth2.Backend.Tests.Services;

public sealed class OidcTokenIssuerTests
{
    [Fact]
    public void Issue_AddsApplicationRolesOnlyToAccessTokenAndUserInfo()
    {
        const string ISSUER = "https://identity.example.com";
        var options = Microsoft.Extensions.Options.Options.Create(
            new OidcProviderOptions { Issuer = ISSUER });
        using var signingKey = new OidcSigningKey(
            options,
            new DevelopmentEnvironment(),
            NullLogger<OidcSigningKey>.Instance);
        var issuer = new OidcTokenIssuer(signingKey, options);

        var response = issuer.Issue(
            new Account
            {
                Id = "user",
                Sub = "subject",
                Name = "User",
                Email = "user@example.com",
                VerifyCode = null,
                CreatedAt = DateTime.UtcNow
            },
            [
                new OAuth2.Repositories.AccountClaim
                {
                    Name = "roles",
                    Value = "untrusted-account-role",
                    CreatedAt = DateTime.UtcNow
                }
            ],
            [],
            ["viewer", "admin", "viewer"],
            new GroupClaimMapping { Format = GroupClaimFormats.Dash, Selectors = [] },
            "application",
            "openid roles",
            DateTimeOffset.UtcNow.AddMinutes(-1).ToUnixTimeSeconds(),
            null);

        Assert.True(signingKey.TryValidate(
            response.AccessToken,
            "at+jwt",
            ISSUER,
            DateTimeOffset.UtcNow,
            out var accessTokenClaims));
        Assert.Equal(
            ["admin", "viewer"],
            accessTokenClaims!["roles"].EnumerateArray().Select(static value => value.GetString()));

        Assert.True(signingKey.TryValidate(
            response.IdToken,
            "JWT",
            ISSUER,
            DateTimeOffset.UtcNow,
            out var idTokenClaims));
        Assert.DoesNotContain("roles", idTokenClaims!);

        Assert.True(issuer.TryGetUserInfo(response.AccessToken, out var userInfo));
        Assert.Contains("roles", userInfo!);
    }

    private sealed class DevelopmentEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "OAuth2.Backend.Tests";

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public string EnvironmentName { get; set; } = "Development";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
