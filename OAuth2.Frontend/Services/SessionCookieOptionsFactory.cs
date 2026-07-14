namespace OAuth2.Services;

internal static class SessionCookieOptionsFactory
{
    public static CookieOptions Create(DateTimeOffset expiresAt)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expiresAt,
            IsEssential = true
        };
    }

    public static CookieOptions CreateDelete()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            IsEssential = true
        };
    }
}
