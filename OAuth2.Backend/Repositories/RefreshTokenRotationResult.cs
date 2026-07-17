namespace OAuth2.Repositories;

public sealed record RefreshTokenRotationResult
{
    public required RefreshTokenRotationStatus Status { get; init; }

    public string? Token { get; init; }

    public string? AccountId { get; init; }

    public string? ClientId { get; init; }

    public string? Scope { get; init; }

    public string? GrantedScope { get; init; }

    public long AuthTime { get; init; }
}
