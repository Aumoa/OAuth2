using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace OAuth2.OpenId;

public sealed record OidcAuthorizationRequest
{
    public string? ClientId { get; init; }

    public string? RedirectUri { get; init; }

    public string? ResponseType { get; init; }

    public string? Scope { get; init; }

    public string? State { get; init; }

    public string? Nonce { get; init; }

    public string? CodeChallenge { get; init; }

    public string? CodeChallengeMethod { get; init; }
}

public static class InternalOidcAuthorization
{
    public const string DefaultClientId = "oauth2";
    public const string RedirectUri = "/api/v1/auth/redirect";
    public const string Scope = "openid profile email";

    private static readonly string[] s_AllowedScopes = ["openid", "profile", "email"];

    public static bool TryValidate(
        OidcAuthorizationRequest? request,
        string clientId,
        [NotNullWhen(true)] out string? normalizedScope,
        [NotNullWhen(false)] out string? error)
    {
        normalizedScope = null;

        if (request is null)
        {
            error = "authorization request is missing";
            return false;
        }

        if (!string.Equals(request.ClientId, clientId, StringComparison.Ordinal))
        {
            error = "invalid_client";
            return false;
        }

        if (!string.Equals(request.RedirectUri, RedirectUri, StringComparison.Ordinal))
        {
            error = "invalid_redirect_uri";
            return false;
        }

        if (!string.Equals(request.ResponseType, "code", StringComparison.Ordinal))
        {
            error = "unsupported_response_type";
            return false;
        }

        if (!TryNormalizeScope(request.Scope, out normalizedScope))
        {
            error = "invalid_scope";
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.State))
        {
            error = "state is required";
            return false;
        }

        if (!Pkce.IsValidChallenge(request.CodeChallenge, request.CodeChallengeMethod))
        {
            error = "PKCE S256 code challenge is required";
            return false;
        }

        error = null;
        return true;
    }

    private static bool TryNormalizeScope(
        string? scope,
        [NotNullWhen(true)] out string? normalizedScope)
    {
        normalizedScope = null;
        if (string.IsNullOrWhiteSpace(scope))
        {
            return false;
        }

        var requestedScopes = scope.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var requestedSet = new HashSet<string>(requestedScopes, StringComparer.Ordinal);
        if (!requestedSet.Contains("openid") || requestedSet.Any(value => !s_AllowedScopes.Contains(value, StringComparer.Ordinal)))
        {
            return false;
        }

        normalizedScope = string.Join(' ', s_AllowedScopes.Where(requestedSet.Contains));
        return true;
    }
}

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

    public static bool Validate(string? codeVerifier, string? codeChallenge, string? codeChallengeMethod)
    {
        if (!IsValidParameter(codeVerifier) || !IsValidChallenge(codeChallenge, codeChallengeMethod))
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
