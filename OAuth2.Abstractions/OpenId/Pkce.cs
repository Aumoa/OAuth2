using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace OAuth2.OpenId;

public static class Pkce
{
    public static string CreateCodeVerifier()
    {
        return Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
    }

    public static string CreateCodeChallenge(string codeVerifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeVerifier);
        return Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
    }

    public static bool Validate(
        string? codeVerifier,
        string? codeChallenge,
        string? codeChallengeMethod)
    {
        if (!IsValidParameter(codeVerifier)
            || !IsValidChallenge(codeChallenge, codeChallengeMethod))
        {
            return false;
        }

        var actualChallenge = Encoding.ASCII.GetBytes(CreateCodeChallenge(codeVerifier));
        var expectedChallenge = Encoding.ASCII.GetBytes(codeChallenge);
        return CryptographicOperations.FixedTimeEquals(actualChallenge, expectedChallenge);
    }

    public static bool IsValidChallenge(
        [NotNullWhen(true)] string? codeChallenge,
        string? codeChallengeMethod)
    {
        return string.Equals(codeChallengeMethod, "S256", StringComparison.Ordinal)
            && IsValidParameter(codeChallenge);
    }

    private static bool IsValidParameter([NotNullWhen(true)] string? value)
    {
        return value is { Length: >= 43 and <= 128 }
            && value.All(static character =>
                character is >= 'A' and <= 'Z'
                || character is >= 'a' and <= 'z'
                || character is >= '0' and <= '9'
                || character is '-' or '.' or '_' or '~');
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
