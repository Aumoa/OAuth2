namespace OAuth2.Options;

public sealed record RememberedSessionOptions
{
    public int LifetimeDays { get; init; } = 14;

    public TimeSpan Lifetime => TimeSpan.FromDays(LifetimeDays);
}
