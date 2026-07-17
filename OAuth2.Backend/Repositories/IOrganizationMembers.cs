using OAuth2.Data;

namespace OAuth2.Repositories;

public interface IOrganizationMembers
{
    Task<(IReadOnlyList<OrganizationMember> Items, long TotalCount)?> GetPageAsync(
        string organizationId,
        string actorAccountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationClaimValue>> GetClaimsAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    Task<OrganizationMemberMutationStatus> AddAsync(
        string organizationId,
        string actorAccountId,
        string accountIdentifier,
        string role,
        CancellationToken cancellationToken = default);

    Task<OrganizationMemberMutationStatus> UpdateRoleAsync(
        string organizationId,
        string actorAccountId,
        string accountId,
        string role,
        CancellationToken cancellationToken = default);

    Task<OrganizationMemberMutationStatus> DeleteAsync(
        string organizationId,
        string actorAccountId,
        string accountId,
        CancellationToken cancellationToken = default);

    Task<OrganizationMemberMutationStatus> TransferOwnershipAsync(
        string organizationId,
        string actorAccountId,
        string accountId,
        CancellationToken cancellationToken = default);
}
