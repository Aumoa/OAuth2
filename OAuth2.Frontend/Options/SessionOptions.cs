namespace OAuth2.Options;

public sealed record SessionOptions
{
    public string CookieName { get; init; } = "__Host-oauth2_session";

    public int LifetimeMinutes { get; init; } = 720;

    public int BrowserLifetimeDays { get; init; } = 90;

    public int MaxRememberedAccounts { get; init; } = 10;

    public TimeSpan Lifetime => TimeSpan.FromMinutes(LifetimeMinutes);

    public TimeSpan BrowserLifetime => TimeSpan.FromDays(BrowserLifetimeDays);
}
