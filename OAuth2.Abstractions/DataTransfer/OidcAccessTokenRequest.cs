namespace OAuth2.DataTransfer;

public sealed record OidcAccessTokenRequest
{
    public required string Token { get; init; }
}
