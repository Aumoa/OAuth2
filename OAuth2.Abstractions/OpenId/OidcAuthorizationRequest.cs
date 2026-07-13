namespace OAuth2.OpenId;

public sealed record OidcAuthorizationRequest
{
    public string? ClientId { get; init; }

    public string? RedirectUri { get; init; }

    public string? ResponseType { get; init; }

    public string? Scope { get; init; }

    public string? State { get; init; }

    public string? Nonce { get; init; }

    public string? CodeChallenge { get; init; }

    public string? CodeChallengeMethod { get; init; }
}
