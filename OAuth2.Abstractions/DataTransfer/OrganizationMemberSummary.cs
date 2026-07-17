namespace OAuth2.DataTransfer;

public sealed record OrganizationMemberSummary
{
    public required string AccountId { get; init; }

    public required string Name { get; init; }

    public required string Role { get; init; }

    public DateTime JoinedAt { get; init; }
}
