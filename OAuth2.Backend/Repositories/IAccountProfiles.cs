using OAuth2.DataTransfer;

namespace OAuth2.Repositories;

public interface IAccountProfiles
{
    Task<AccountProfile?> GetAsync(
        string accountId,
        CancellationToken cancellationToken = default);

    Task<AccountProfile?> UpdateAsync(
        string accountId,
        UpdateAccountProfileForm form,
        CancellationToken cancellationToken = default);
}
