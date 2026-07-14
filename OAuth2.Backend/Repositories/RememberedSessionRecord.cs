namespace OAuth2.Repositories;

public sealed record RememberedSessionRecord
{
    public required string AccountId { get; init; }

    public required long AuthTime { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
