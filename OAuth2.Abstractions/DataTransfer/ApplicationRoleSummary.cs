namespace OAuth2.DataTransfer;

public sealed record ApplicationRoleSummary
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public long MemberCount { get; init; }

    public DateTime CreatedAt { get; init; }
}
