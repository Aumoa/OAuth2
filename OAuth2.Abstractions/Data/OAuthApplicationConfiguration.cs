namespace OAuth2.Data;

public sealed record OAuthApplicationConfiguration
{
    public required OAuthApplication Application { get; init; }

    public required IReadOnlyList<string> RedirectUris { get; init; }

    public required IReadOnlyList<string> AllowedScopes { get; init; }

    public GroupClaimMapping GroupClaimMapping { get; init; } = new()
    {
        Format = GroupClaimFormats.Dash,
        Selectors = []
    };
}
