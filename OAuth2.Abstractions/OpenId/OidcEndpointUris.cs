using System.Diagnostics.CodeAnalysis;

namespace OAuth2.OpenId;

public static class OidcEndpointUris
{
    public static bool TryNormalizeIssuer(
        string? value,
        [NotNullWhen(true)] out string? issuer)
    {
        issuer = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !string.IsNullOrEmpty(uri.UserInfo)
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment)
            || uri.AbsolutePath != "/"
            || (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                && (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
                    || !uri.IsLoopback)))
        {
            return false;
        }

        issuer = uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return true;
    }

    public static string Authorization(string issuer) => Endpoint(issuer, "/authorize");

    public static string Token(string issuer) => Endpoint(issuer, "/token");

    public static string UserInfo(string issuer) => Endpoint(issuer, "/userinfo");

    public static string JsonWebKeys(string issuer) => Endpoint(issuer, "/jwks");

    private static string Endpoint(string issuer, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(issuer);
        return issuer.TrimEnd('/') + path;
    }
}
