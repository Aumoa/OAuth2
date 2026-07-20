namespace OAuth2.Data;

public static class GroupClaimFormats
{
    public const string Dash = "dash";

    public const string Path = "path";

    public const string Colon = "colon";

    public static IReadOnlySet<string> Supported { get; } = new HashSet<string>(
        [Dash, Path, Colon],
        StringComparer.Ordinal);
}
