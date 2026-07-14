namespace OAuth2.Repositories;

public sealed record AccountClaim
{
    public required string Name { get; init; }

    public required string Value { get; init; }

    public required DateTime CreatedAt { get; init; }
}
