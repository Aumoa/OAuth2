namespace OAuth2.Services;

internal sealed record BrowserSessionRecord
{
    public string? ActiveAccountKey { get; init; }

    public DateTimeOffset? ActiveExpiresAt { get; init; }

    public required DateTimeOffset ExpiresAt { get; init; }

    public required List<RememberedAccountRecord> Accounts { get; init; }
}
