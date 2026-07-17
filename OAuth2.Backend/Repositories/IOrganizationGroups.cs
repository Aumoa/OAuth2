using OAuth2.Data;

namespace OAuth2.Repositories;

public interface IOrganizationGroups
{
    Task<IReadOnlyList<(OrganizationGroup Group, long MemberCount)>?> GetGroupsAsync(
        string organizationId,
        string actorAccountId,
        CancellationToken cancellationToken = default);

    Task<(OrganizationGroup Group, long MemberCount)?> GetGroupAsync(
        string organizationId,
        string groupId,
        string actorAccountId,
        CancellationToken cancellationToken = default);

    Task<(OrganizationGroupMutationStatus Status, OrganizationGroup? Group)> AddGroupAsync(
        string organizationId,
        string actorAccountId,
        string groupId,
        string name,
        CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<OrganizationMember> Items, long TotalCount)?> GetMembersAsync(
        string organizationId,
        string groupId,
        string actorAccountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<OrganizationGroupMutationStatus> AddMemberAsync(
        string organizationId,
        string groupId,
        string actorAccountId,
        string accountId,
        CancellationToken cancellationToken = default);

    Task<OrganizationGroupMutationStatus> DeleteMemberAsync(
        string organizationId,
        string groupId,
        string actorAccountId,
        string accountId,
        CancellationToken cancellationToken = default);
}
