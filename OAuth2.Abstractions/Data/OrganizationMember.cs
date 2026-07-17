namespace OAuth2.Data;

public sealed record OrganizationMember
{
    public required string AccountId { get; init; }

    public required string Name { get; init; }

    public required string Role { get; init; }

    public DateTime JoinedAt { get; init; }
}
