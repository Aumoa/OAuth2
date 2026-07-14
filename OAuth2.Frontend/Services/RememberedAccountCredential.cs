namespace OAuth2.Services;

public sealed record RememberedAccountCredential
{
    public required string Id { get; init; }

    public string? Token { get; init; }
}
