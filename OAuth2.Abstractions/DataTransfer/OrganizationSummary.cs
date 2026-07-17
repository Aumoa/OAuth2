namespace OAuth2.DataTransfer;

public sealed record OrganizationSummary
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Role { get; init; }

    public DateTime CreatedAt { get; init; }
}
