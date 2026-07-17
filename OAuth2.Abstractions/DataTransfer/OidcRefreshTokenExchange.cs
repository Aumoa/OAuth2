namespace OAuth2.DataTransfer;

public sealed record OidcRefreshTokenExchange
{
    public required string RefreshToken { get; init; }

    public required string ClientId { get; init; }

    public string? ClientSecret { get; init; }

    public string? Scope { get; init; }
}
