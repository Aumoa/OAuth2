using OAuth2.Data;

namespace OAuth2.Repositories;

public interface IOrganizations
{
    Task<OrganizationMembership?> AddOrganizationAsync(
        string id,
        string name,
        string accountId,
        CancellationToken cancellationToken = default);

    Task<OrganizationMembership?> GetOrganizationAsync(
        string id,
        string accountId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationMembership>> GetOrganizationsAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
