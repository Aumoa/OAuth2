namespace OAuth2.DataTransfer;

public sealed record RememberedSessionRevocation
{
    public required string Token { get; init; }
}
