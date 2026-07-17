namespace OAuth2.Data;

public sealed record OAuthOrganization
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public DateTime CreatedAt { get; init; }
}
