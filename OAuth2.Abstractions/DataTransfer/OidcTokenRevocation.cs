namespace OAuth2.DataTransfer;

public sealed record OidcTokenRevocation
{
    public required string Token { get; init; }

    public required string ClientId { get; init; }

    public string? ClientSecret { get; init; }
}
