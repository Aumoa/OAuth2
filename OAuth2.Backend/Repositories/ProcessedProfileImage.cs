namespace OAuth2.Repositories;

public sealed record ProcessedProfileImage
{
    public required byte[] Data { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required byte[] Hash { get; init; }

    public string Version => Convert.ToHexStringLower(Hash);
}
