namespace OAuth2.Data;

public sealed record OAuthApplication
{
    public required string Id { get; init; }

    public required string OwnerId { get; init; }

    public required string Name { get; init; }

    public DateTime CreatedAt { get; init; }
}
