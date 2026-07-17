using System.Security.Cryptography;
using System.Text.Json;
using OAuth2.OpenId;

namespace OAuth2.Abstractions.Tests.OpenId;

public sealed class OidcJwtTests
{
    private const string Issuer = "https://sso.example";

    [Fact]
    public void CreateAndTryValidate_RoundTripsRs256Token()
    {
        using var key = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var token = OidcJwt.Create(
            key,
            "test-key",
            "JWT",
            CreateClaims(now));

        var result = OidcJwt.TryValidate(
            token,
            key,
            "test-key",
            "JWT",
            Issuer,
            now,
            out var claims);

        Assert.True(result);
        Assert.NotNull(claims);
        Assert.Equal("subject", claims["sub"].GetString());
    }

    [Fact]
    public void TryValidate_RejectsTokenSignedByAnotherKey()
    {
        using var signingKey = RSA.Create(2048);
        using var validationKey = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var token = OidcJwt.Create(
            signingKey,
            "test-key",
            "at+jwt",
            CreateClaims(now));

        Assert.False(OidcJwt.TryValidate(
            token,
            validationKey,
            "test-key",
            "at+jwt",
            Issuer,
            now,
            out _));
    }

    [Fact]
    public void TryValidate_RejectsExpiredToken()
    {
        using var key = RSA.Create(2048);
        var now = DateTimeOffset.UtcNow;
        var claims = CreateClaims(now);
        claims["exp"] = now.AddSeconds(-1).ToUnixTimeSeconds();
        var token = OidcJwt.Create(key, "test-key", "JWT", claims);

        Assert.False(OidcJwt.TryValidate(
            token,
            key,
            "test-key",
            "JWT",
            Issuer,
            now,
            out _));
    }

    private static Dictionary<string, object?> CreateClaims(DateTimeOffset now) =>
        new(StringComparer.Ordinal)
        {
            ["iss"] = Issuer,
            ["sub"] = "subject",
            ["aud"] = "client",
            ["iat"] = now.ToUnixTimeSeconds(),
            ["exp"] = now.AddMinutes(5).ToUnixTimeSeconds(),
            ["profile"] = JsonSerializer.SerializeToElement(new { name = "User" })
        };
}
