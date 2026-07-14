namespace OAuth2.Services;

public sealed record RememberedAccount
{
    public required string AccountKey { get; init; }

    public required string Id { get; init; }

    public string? Name { get; init; }

    public string? Email { get; init; }

    public string? Picture { get; init; }

    public required bool CanSignIn { get; init; }

    public required DateTimeOffset AuthenticatedAt { get; init; }
}
