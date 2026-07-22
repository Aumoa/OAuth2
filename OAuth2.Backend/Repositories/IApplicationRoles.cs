using OAuth2.Data;

namespace OAuth2.Repositories;

public interface IApplicationRoles
{
    const int MaxRoles = 100;

    Task<IReadOnlyList<(OAuthApplicationRole Role, long MemberCount)>?> GetRolesAsync(
        string clientId,
        string ownerId,
        CancellationToken cancellationToken = default);

    Task<(ApplicationRoleMutationStatus Status, OAuthApplicationRole? Role)> AddRoleAsync(
        string clientId,
        string ownerId,
        string roleId,
        string name,
        CancellationToken cancellationToken = default);

    Task<ApplicationRoleMutationStatus> DeleteRoleAsync(
        string clientId,
        string ownerId,
        string roleId,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<OAuthApplicationRoleMember> Items, long TotalCount)?> GetMembersAsync(
        string clientId,
        string ownerId,
        string roleId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ApplicationRoleMutationStatus> AddMemberAsync(
        string clientId,
        string ownerId,
        string roleId,
        string accountIdentifier,
        CancellationToken cancellationToken = default);

    Task<ApplicationRoleMutationStatus> DeleteMemberAsync(
        string clientId,
        string ownerId,
        string roleId,
        string accountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetAssignedRoleIdsAsync(
        string clientId,
        string accountId,
        CancellationToken cancellationToken = default);
}
