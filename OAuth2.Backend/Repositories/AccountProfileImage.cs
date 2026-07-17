namespace OAuth2.Repositories;

public sealed record AccountProfileImage
{
    public required byte[] Data { get; init; }

    public required string ContentType { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required byte[] Hash { get; init; }

    public required DateTime UpdatedAt { get; init; }

    public string Version => Convert.ToHexStringLower(Hash);

    public string EntityTag => $"\"{Version}\"";
}
