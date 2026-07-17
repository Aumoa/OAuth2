using OAuth2.Data;

namespace OAuth2.OpenId;

public static class OidcRedirectUriPolicy
{
    public static bool IsValidForAnyApplicationType(string? value)
    {
        return IsValidRegistration(value, OAuthApplicationTypes.Web)
            || IsValidRegistration(value, OAuthApplicationTypes.Android)
            || IsValidRegistration(value, OAuthApplicationTypes.Ios)
            || IsValidRegistration(value, OAuthApplicationTypes.Desktop);
    }

    public static bool IsValidRegistration(
        string? value,
        string applicationType)
    {
        if (!TryParse(value, out var uri))
        {
            return false;
        }

        return applicationType switch
        {
            OAuthApplicationTypes.Web =>
                IsHttps(uri) || IsWebDevelopmentLoopback(uri),
            OAuthApplicationTypes.Android or OAuthApplicationTypes.Ios =>
                IsHttps(uri) || IsPrivateUseScheme(value!, uri),
            OAuthApplicationTypes.Desktop =>
                IsHttps(uri)
                || IsPrivateUseScheme(value!, uri)
                || IsDesktopLoopbackRegistration(uri),
            _ => false
        };
    }

    public static bool Matches(
        string? requestedRedirectUri,
        string registeredRedirectUri,
        string applicationType)
    {
        if (string.IsNullOrWhiteSpace(requestedRedirectUri)
            || !IsValidRegistration(registeredRedirectUri, applicationType))
        {
            return false;
        }

        if (applicationType == OAuthApplicationTypes.Desktop
            && Uri.TryCreate(registeredRedirectUri, UriKind.Absolute, out var registered)
            && IsDesktopLoopbackRegistration(registered))
        {
            return Uri.TryCreate(requestedRedirectUri, UriKind.Absolute, out var requested)
                && IsDesktopLoopbackRequest(requested)
                && string.Equals(
                    registered.Scheme,
                    requested.Scheme,
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    registered.Host,
                    requested.Host,
                    StringComparison.OrdinalIgnoreCase)
                && string.Equals(
                    registered.GetComponents(
                        UriComponents.PathAndQuery,
                        UriFormat.UriEscaped),
                    requested.GetComponents(
                        UriComponents.PathAndQuery,
                        UriFormat.UriEscaped),
                    StringComparison.Ordinal);
        }

        return string.Equals(
            requestedRedirectUri,
            registeredRedirectUri,
            StringComparison.Ordinal);
    }

    private static bool TryParse(string? value, out Uri uri)
    {
        uri = null!;
        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(value, UriKind.Absolute, out var parsed)
            || string.IsNullOrEmpty(parsed.UserInfo) is false
            || string.IsNullOrEmpty(parsed.Fragment) is false)
        {
            return false;
        }

        uri = parsed;
        return true;
    }

    private static bool IsHttps(Uri uri)
    {
        return string.Equals(
                uri.Scheme,
                Uri.UriSchemeHttps,
                StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(uri.Host);
    }

    private static bool IsWebDevelopmentLoopback(Uri uri)
    {
        return string.Equals(
                uri.Scheme,
                Uri.UriSchemeHttp,
                StringComparison.OrdinalIgnoreCase)
            && uri.IsLoopback;
    }

    private static bool IsDesktopLoopbackRegistration(Uri uri)
    {
        return IsDesktopLoopbackRequest(uri) && uri.IsDefaultPort;
    }

    private static bool IsDesktopLoopbackRequest(Uri uri)
    {
        return string.Equals(
                uri.Scheme,
                Uri.UriSchemeHttp,
                StringComparison.OrdinalIgnoreCase)
            && (string.Equals(uri.Host, "127.0.0.1", StringComparison.Ordinal)
                || string.Equals(uri.Host, "[::1]", StringComparison.Ordinal)
                || string.Equals(uri.Host, "::1", StringComparison.Ordinal));
    }

    private static bool IsPrivateUseScheme(string value, Uri uri)
    {
        if (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !uri.Scheme.Contains('.')
            || !string.IsNullOrEmpty(uri.Host)
            || !uri.AbsolutePath.StartsWith("/", StringComparison.Ordinal))
        {
            return false;
        }

        var schemeSeparator = value.IndexOf(':');
        return schemeSeparator > 0
            && value.AsSpan(schemeSeparator).StartsWith(":/", StringComparison.Ordinal)
            && !value.AsSpan(schemeSeparator).StartsWith("://", StringComparison.Ordinal);
    }
}
