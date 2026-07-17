namespace OAuth2.Data;

public sealed record OrganizationGroup
{
    public required string OrganizationId { get; init; }

    public required string Id { get; init; }

    public required string Name { get; init; }

    public DateTime CreatedAt { get; init; }
}
