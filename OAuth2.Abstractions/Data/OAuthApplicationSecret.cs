namespace OAuth2.Data;

public sealed record OAuthApplicationSecret
{
    public long Id { get; init; }

    public required string ClientId { get; init; }

    public required string Prefix { get; init; }

    public DateTime CreatedAt { get; init; }
}
