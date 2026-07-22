namespace OAuth2.DataTransfer;

public sealed record ApplicationRoleMemberSummary
{
    public required string AccountId { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }

    public DateTime AssignedAt { get; init; }
}
