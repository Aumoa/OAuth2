using System.Text.Json;

namespace OAuth2.DataTransfer;

public sealed record GrantedUserInfo
{
    public required string Scope { get; init; }

    public required Dictionary<string, JsonElement> Claims { get; init; }
}
