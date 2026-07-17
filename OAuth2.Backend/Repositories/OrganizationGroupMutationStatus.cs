namespace OAuth2.Repositories;

public enum OrganizationGroupMutationStatus
{
    Succeeded,
    OrganizationNotFound,
    GroupNotFound,
    MemberNotFound,
    GroupExists,
    AlreadyMember,
    Forbidden
}
