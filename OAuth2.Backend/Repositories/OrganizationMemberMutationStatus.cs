namespace OAuth2.Repositories;

public enum OrganizationMemberMutationStatus
{
    Succeeded,
    OrganizationNotFound,
    AccountNotFound,
    MemberNotFound,
    AlreadyMember,
    Forbidden
}
