using System.Text.Json;

namespace OAuth2.Services;

public sealed record SessionRecord
{
    public required string GrantedScope { get; init; }

    public required string SessionScope { get; init; }

    public required Dictionary<string, JsonElement> Claims { get; init; }
}
