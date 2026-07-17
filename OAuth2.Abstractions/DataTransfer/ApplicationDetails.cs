namespace OAuth2.DataTransfer;

public sealed record ApplicationDetails
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string ApplicationType { get; init; }

    public DateTime CreatedAt { get; init; }

    public required IReadOnlyList<string> RedirectUris { get; init; }

    public required IReadOnlyList<string> AllowedScopes { get; init; }
}
