namespace OAuth2.DataTransfer;

public sealed record AccountProfileClaim
{
    public required string Name { get; init; }

    public required string Value { get; init; }
}
