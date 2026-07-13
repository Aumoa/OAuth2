namespace OAuth2.DataTransfer;

public sealed record SessionUser
{
    public required string Id { get; init; }

    public required string Sub { get; init; }

    public required string Email { get; init; }

    public required string Name { get; init; }
}
