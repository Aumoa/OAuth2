namespace OAuth2.OpenId;

public sealed record OidcProviderOptions
{
    public required string Issuer { get; init; }

    public int AccessTokenLifetimeMinutes { get; init; } = 30;

    public string? SigningKeyPath { get; init; }
}
