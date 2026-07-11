using OAuth2.Data;

namespace OAuth2.Repositories;

public sealed record AccountRegistration(string Sub, string VerifyCode);

public interface IAccounts
{
    Task<Account?> GetAccountAsync(string id, CancellationToken cancellationToken = default);

    Task<AccountRegistration?> AddAccountAsync(
        string id,
        string password,
        string fullName,
        string email,
        CancellationToken cancellationToken = default);
}
