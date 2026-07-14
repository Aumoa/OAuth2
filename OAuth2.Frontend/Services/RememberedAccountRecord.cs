using System.Text.Json;

namespace OAuth2.Services;

internal sealed record RememberedAccountRecord
{
    public required string Key { get; init; }

    public required string GrantedScope { get; init; }

    public required string SessionScope { get; init; }

    public required Dictionary<string, JsonElement> Claims { get; init; }

    public string? Token { get; init; }

    public DateTimeOffset? TokenExpiresAt { get; init; }

    public required DateTimeOffset AuthenticatedAt { get; init; }

    public required DateTimeOffset LastUsedAt { get; init; }
}
