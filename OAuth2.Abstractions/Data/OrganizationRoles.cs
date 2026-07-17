namespace OAuth2.Data;

public static class OrganizationRoles
{
    public const string Owner = "owner";

    public const string Admin = "admin";

    public const string Member = "member";

    public static readonly string[] All = [Owner, Admin, Member];

    public static readonly string[] Assignable = [Admin, Member];

    public static bool IsSupported(string? role) =>
        role is Owner or Admin or Member;

    public static bool IsAssignable(string? role) =>
        role is Admin or Member;

    public static bool CanManage(string? actorRole, string? targetRole) =>
        Rank(actorRole) > Rank(targetRole);

    public static bool CanAdd(string? actorRole, string? role) =>
        IsAssignable(role) && CanManage(actorRole, role);

    public static bool CanAssign(string? actorRole, string? role) =>
        IsAssignable(role) && Rank(actorRole) >= Rank(role);

    private static int Rank(string? role) => role switch
    {
        Owner => 3,
        Admin => 2,
        Member => 1,
        _ => 0
    };
}
