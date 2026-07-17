namespace OAuth2.DataTransfer;

public sealed record ProfileImageReference
{
    public required string Picture { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }
}
