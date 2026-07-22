namespace OAuth2.Data;

public sealed record OAuthApplicationRoleMember
{
    public required string AccountId { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }

    public DateTime AssignedAt { get; init; }
}
