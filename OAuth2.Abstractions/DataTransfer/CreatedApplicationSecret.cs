namespace OAuth2.DataTransfer;

public sealed record CreatedApplicationSecret
{
    public long Id { get; init; }

    public required string Prefix { get; init; }

    public DateTime CreatedAt { get; init; }

    public required string Secret { get; init; }
}
