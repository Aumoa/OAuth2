namespace OAuth2.Data;

public sealed record OAuthApplicationConfiguration
{
    public required OAuthApplication Application { get; init; }

    public required IReadOnlyList<string> RedirectUris { get; init; }

    public required IReadOnlyList<string> AllowedScopes { get; init; }
}
