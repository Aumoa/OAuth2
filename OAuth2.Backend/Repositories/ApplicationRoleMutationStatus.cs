namespace OAuth2.Repositories;

public enum ApplicationRoleMutationStatus
{
    Succeeded,
    ApplicationNotFound,
    RoleNotFound,
    AccountNotFound,
    RoleExists,
    AlreadyAssigned,
    RoleLimitReached
}
