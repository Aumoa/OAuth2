using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OAuth2.Data;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Repositories;

namespace OAuth2.Services;

public sealed class OidcTokenIssuer(
    OidcSigningKey signingKey,
    IOptions<OidcProviderOptions> options)
{
    public OidcTokenResponse Issue(
        Account account,
        IReadOnlyList<AccountClaim> accountClaims,
        AuthorizationCodeBody authorizationCode)
    {
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(options.Value.AccessTokenLifetimeMinutes);
        var userClaims = OidcUserInfoFactory.Create(
            account,
            accountClaims,
            authorizationCode.Scope);

        var accessTokenClaims = CreateTokenClaims(
            userClaims,
            authorizationCode,
            now,
            expiresAt);
        accessTokenClaims["scope"] = authorizationCode.Scope;
        accessTokenClaims["client_id"] = authorizationCode.ClientId;
        accessTokenClaims["jti"] = OidcJwt.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(16));

        var idTokenClaims = CreateTokenClaims(
            userClaims,
            authorizationCode,
            now,
            expiresAt);
        if (!string.IsNullOrWhiteSpace(authorizationCode.Nonce))
        {
            idTokenClaims["nonce"] = authorizationCode.Nonce;
        }

        return new OidcTokenResponse
        {
            AccessToken = signingKey.CreateToken("at+jwt", accessTokenClaims),
            ExpiresIn = (long)(expiresAt - now).TotalSeconds,
            IdToken = signingKey.CreateToken("JWT", idTokenClaims),
            Scope = authorizationCode.Scope
        };
    }

    public bool TryGetUserInfo(
        string? accessToken,
        out Dictionary<string, JsonElement>? userInfo)
    {
        userInfo = null;
        if (!signingKey.TryValidate(
                accessToken,
                "at+jwt",
                GetIssuer(),
                DateTimeOffset.UtcNow,
                out var claims)
            || claims is null
            || !TryGetString(claims, "sub", out _)
            || !TryGetString(claims, "client_id", out var clientId)
            || !TryGetString(claims, "aud", out var audience)
            || !string.Equals(clientId, audience, StringComparison.Ordinal)
            || !TryGetString(claims, "scope", out var scope)
            || !OidcScopePolicy.TryNormalize(scope, false, out var normalizedScope)
            || !OidcScopePolicy.Split(normalizedScope)
                .Contains(OidcScopePolicy.OpenIdScope, StringComparer.Ordinal))
        {
            return false;
        }

        userInfo = OidcClaimPolicy.Filter(claims, normalizedScope);
        return userInfo.ContainsKey("sub");
    }

    private Dictionary<string, object?> CreateTokenClaims(
        IReadOnlyDictionary<string, JsonElement> userClaims,
        AuthorizationCodeBody authorizationCode,
        DateTimeOffset now,
        DateTimeOffset expiresAt)
    {
        var claims = userClaims.ToDictionary(
            static pair => pair.Key,
            static pair => (object?)pair.Value,
            StringComparer.Ordinal);
        claims["iss"] = GetIssuer();
        claims["aud"] = authorizationCode.ClientId;
        claims["iat"] = now.ToUnixTimeSeconds();
        claims["exp"] = expiresAt.ToUnixTimeSeconds();
        claims["auth_time"] = authorizationCode.AuthTime;
        return claims;
    }

    private string GetIssuer()
    {
        if (!OidcEndpointUris.TryNormalizeIssuer(options.Value.Issuer, out var issuer))
        {
            throw new InvalidOperationException("The configured OpenID issuer is invalid.");
        }

        return issuer;
    }

    private static bool TryGetString(
        IReadOnlyDictionary<string, JsonElement> claims,
        string name,
        out string? value)
    {
        value = null;
        if (!claims.TryGetValue(name, out var claim)
            || claim.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        value = claim.GetString();
        return !string.IsNullOrWhiteSpace(value);
    }
}
