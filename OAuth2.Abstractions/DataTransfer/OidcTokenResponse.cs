using System.Text.Json.Serialization;

namespace OAuth2.DataTransfer;

public sealed record OidcTokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public required long ExpiresIn { get; init; }

    [JsonPropertyName("id_token")]
    public required string IdToken { get; init; }

    [JsonPropertyName("scope")]
    public required string Scope { get; init; }
}
