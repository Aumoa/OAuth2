namespace OAuth2.DataTransfer;

public sealed record RememberedSessionGrant
{
    public required string Token { get; init; }

    public required DateTimeOffset AuthenticatedAt { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }
}
