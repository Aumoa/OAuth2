namespace OAuth2.Repositories;

public interface IAccountProfileImages
{
    Task<AccountProfileImage?> GetAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    Task<bool> UpsertAsync(
        string accountId,
        ProcessedProfileImage image,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
