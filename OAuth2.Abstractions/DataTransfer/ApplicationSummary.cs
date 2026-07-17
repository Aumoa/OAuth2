namespace OAuth2.DataTransfer;

public sealed record ApplicationSummary
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string ApplicationType { get; init; }

    public DateTime CreatedAt { get; init; }
}
