namespace OAuth2.Data;

public sealed record OrganizationClaimValue
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Role { get; init; }

    public IReadOnlyList<string> GroupIds { get; init; } = [];
}
