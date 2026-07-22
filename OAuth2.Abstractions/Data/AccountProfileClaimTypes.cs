namespace OAuth2.Data;

public static class AccountProfileClaimTypes
{
    public const string Nickname = "nickname";

    public static IReadOnlyList<string> Additional { get; } =
    [
        "family_name",
        "given_name",
        "middle_name",
        "profile",
        "website",
        "gender",
        "birthdate",
        "zoneinfo",
        "locale",
        "phone_number"
    ];

    public static IReadOnlyList<string> Editable { get; } =
        [Nickname, .. Additional];

    public static bool IsAdditional(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && Additional.Contains(name, StringComparer.Ordinal);
}
