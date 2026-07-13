namespace OAuth2.Options;

public sealed record SessionOptions
{
    public string CookieName { get; init; } = "oauth2_session";

    public int LifetimeMinutes { get; init; } = 720;

    public TimeSpan Lifetime => TimeSpan.FromMinutes(LifetimeMinutes);
}
