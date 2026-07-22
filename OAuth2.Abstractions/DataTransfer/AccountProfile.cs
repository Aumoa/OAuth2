namespace OAuth2.DataTransfer;

public sealed record AccountProfile
{
    public required string FullName { get; init; }

    public string? Nickname { get; init; }

    public required IReadOnlyList<AccountProfileClaim> Claims { get; init; }

    public required long UpdatedAt { get; init; }
}
