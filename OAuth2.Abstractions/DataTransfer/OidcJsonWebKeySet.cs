using System.Text.Json.Serialization;

namespace OAuth2.DataTransfer;

public sealed record OidcJsonWebKeySet
{
    [JsonPropertyName("keys")]
    public required IReadOnlyList<OidcJsonWebKey> Keys { get; init; }
}

public sealed record OidcJsonWebKey
{
    [JsonPropertyName("kty")]
    public string KeyType { get; init; } = "RSA";

    [JsonPropertyName("use")]
    public string Use { get; init; } = "sig";

    [JsonPropertyName("kid")]
    public required string KeyId { get; init; }

    [JsonPropertyName("alg")]
    public string Algorithm { get; init; } = "RS256";

    [JsonPropertyName("n")]
    public required string Modulus { get; init; }

    [JsonPropertyName("e")]
    public required string Exponent { get; init; }
}
