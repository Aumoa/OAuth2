namespace OAuth2.Data;

public sealed record GeneratedOAuthApplicationSecret
{
    public required OAuthApplicationSecret ApplicationSecret { get; init; }

    public required string Value { get; init; }
}
