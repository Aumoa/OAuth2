using OAuth2.OpenId;

namespace OAuth2.Services;

internal static class OidcFlowCookies
{
    public const string VerifierName = "oauth2_pkce_verifier";
    public const string StateName = "oauth2_pkce_state";

    public static CookieOptions Create()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = InternalOidcAuthorization.CallbackPath,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10)
        };
    }

    public static CookieOptions CreateDelete()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = InternalOidcAuthorization.CallbackPath
        };
    }
}
