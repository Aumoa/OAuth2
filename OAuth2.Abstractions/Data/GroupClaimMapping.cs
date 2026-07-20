namespace OAuth2.Data;

public sealed record GroupClaimMapping
{
    public required string Format { get; init; }

    public required IReadOnlyList<string> Selectors { get; init; }
}
