using OAuth2.Data;

namespace OAuth2.Repositories;

public interface IAccounts
{
    Task<Account?> GetAccountAsync(string id, CancellationToken cancellationToken = default);

    Task<string?> AddAccountAsync(string id, string password, string fullName, string email, CancellationToken cancellationToken = default);
}
