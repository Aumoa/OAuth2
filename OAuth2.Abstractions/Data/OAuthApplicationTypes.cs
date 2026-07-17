namespace OAuth2.Data;

public static class OAuthApplicationTypes
{
    public const string Web = "web";

    public const string Android = "android";

    public const string Ios = "ios";

    public const string Desktop = "desktop";

    public static IReadOnlyList<string> All { get; } =
        [Web, Android, Ios, Desktop];

    public static bool IsSupported(string? value)
    {
        return value is Web or Android or Ios or Desktop;
    }
}
