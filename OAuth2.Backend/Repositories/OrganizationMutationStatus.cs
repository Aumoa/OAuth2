namespace OAuth2.Repositories;

public enum OrganizationMutationStatus
{
    Succeeded,
    OrganizationNotFound,
    Forbidden,
    ConfirmationMismatch,
    ApplicationOwnerConflict
}
