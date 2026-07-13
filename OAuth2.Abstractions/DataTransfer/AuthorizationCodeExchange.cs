namespace OAuth2.DataTransfer;

public sealed record AuthorizationCodeExchange
{
    public required string Code { get; init; }

    public required string ClientId { get; init; }

    public required string RedirectUri { get; init; }

    public required string CodeVerifier { get; init; }
}
