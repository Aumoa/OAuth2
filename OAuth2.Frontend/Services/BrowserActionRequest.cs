namespace OAuth2.Services;

internal static class BrowserActionRequest
{
    public const string HeaderName = "X-OAuth2-Action";
    public const string HeaderValue = "1";

    public static bool IsValid(HttpRequest request)
    {
        return request.Headers.TryGetValue(HeaderName, out var value)
            && value.Count == 1
            && string.Equals(value[0], HeaderValue, StringComparison.Ordinal);
    }
}
