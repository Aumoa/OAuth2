using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OAuth2.OpenId;

public static class OidcJwt
{
    private const int MaxTokenLength = 64 * 1024;

    public static string Create(
        RSA signingKey,
        string keyId,
        string type,
        IReadOnlyDictionary<string, object?> claims)
    {
        ArgumentNullException.ThrowIfNull(signingKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(keyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(claims);

        var header = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["alg"] = "RS256",
            ["kid"] = keyId,
            ["typ"] = type
        };
        var encodedHeader = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(header));
        var encodedPayload = Base64UrlEncode(JsonSerializer.SerializeToUtf8Bytes(claims));
        var signingInput = Encoding.ASCII.GetBytes($"{encodedHeader}.{encodedPayload}");
        var signature = signingKey.SignData(
            signingInput,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        return $"{encodedHeader}.{encodedPayload}.{Base64UrlEncode(signature)}";
    }

    public static bool TryValidate(
        string? token,
        RSA verificationKey,
        string expectedKeyId,
        string expectedType,
        string expectedIssuer,
        DateTimeOffset now,
        [NotNullWhen(true)] out Dictionary<string, JsonElement>? claims)
    {
        ArgumentNullException.ThrowIfNull(verificationKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedKeyId);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedType);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedIssuer);

        claims = null;
        if (string.IsNullOrWhiteSpace(token) || token.Length > MaxTokenLength)
        {
            return false;
        }

        var segments = token.Split('.');
        if (segments.Length != 3
            || !TryBase64UrlDecode(segments[0], out var headerBytes)
            || !TryBase64UrlDecode(segments[1], out var payloadBytes)
            || !TryBase64UrlDecode(segments[2], out var signature))
        {
            return false;
        }

        try
        {
            using var headerDocument = JsonDocument.Parse(headerBytes);
            var header = headerDocument.RootElement;
            if (header.ValueKind != JsonValueKind.Object
                || !HasStringValue(header, "alg", "RS256")
                || !HasStringValue(header, "kid", expectedKeyId)
                || !HasStringValue(header, "typ", expectedType))
            {
                return false;
            }

            var signingInput = Encoding.ASCII.GetBytes($"{segments[0]}.{segments[1]}");
            if (!verificationKey.VerifyData(
                signingInput,
                signature,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1))
            {
                return false;
            }

            using var payloadDocument = JsonDocument.Parse(payloadBytes);
            var payload = payloadDocument.RootElement;
            var nowSeconds = now.ToUnixTimeSeconds();
            if (payload.ValueKind != JsonValueKind.Object
                || !HasStringValue(payload, "iss", expectedIssuer)
                || !payload.TryGetProperty("exp", out var expiresAt)
                || !expiresAt.TryGetInt64(out var expiresAtSeconds)
                || expiresAtSeconds <= nowSeconds
                || !payload.TryGetProperty("iat", out var issuedAt)
                || !issuedAt.TryGetInt64(out var issuedAtSeconds)
                || issuedAtSeconds > now.AddMinutes(5).ToUnixTimeSeconds())
            {
                return false;
            }

            claims = payload.EnumerateObject().ToDictionary(
                static property => property.Name,
                static property => property.Value.Clone(),
                StringComparer.Ordinal);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
        catch (CryptographicException)
        {
            return false;
        }
    }

    public static string Base64UrlEncode(ReadOnlySpan<byte> value)
    {
        return Convert.ToBase64String(value)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static bool HasStringValue(JsonElement value, string propertyName, string expected)
    {
        return value.TryGetProperty(propertyName, out var property)
            && property.ValueKind == JsonValueKind.String
            && string.Equals(property.GetString(), expected, StringComparison.Ordinal);
    }

    private static bool TryBase64UrlDecode(
        string value,
        [NotNullWhen(true)] out byte[]? bytes)
    {
        bytes = null;
        if (value.Length == 0
            || value.Length % 4 == 1
            || value.Any(static character =>
                character is not (>= 'A' and <= 'Z')
                and not (>= 'a' and <= 'z')
                and not (>= '0' and <= '9')
                and not '-' and not '_'))
        {
            return false;
        }

        var base64 = value.Replace('-', '+').Replace('_', '/');
        base64 += new string('=', (4 - base64.Length % 4) % 4);
        try
        {
            bytes = Convert.FromBase64String(base64);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
