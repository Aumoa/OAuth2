namespace OAuth2.DataTransfer;

public sealed record AuthorizationCodeExchange
{
    public required string Code { get; init; }

    public required string ClientId { get; init; }

    public required string RedirectUri { get; init; }

    public required string CodeVerifier { get; init; }
}

public sealed record SessionUser
{
    public required string Id { get; init; }

    public required string Sub { get; init; }

    public required string Email { get; init; }

    public required string Name { get; init; }
}
