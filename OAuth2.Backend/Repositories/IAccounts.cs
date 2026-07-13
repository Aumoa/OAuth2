using OAuth2.Data;

namespace OAuth2.Repositories;

public sealed record AccountRegistration(string Sub, string VerifyCode);

public sealed record AccountLogin(string Sub, bool EmailVerified);

public sealed record EmailVerificationDelivery(string Sub, string VerifyCode, string Email);

public interface IAccounts
{
    Task<Account?> GetAccountAsync(string id, CancellationToken cancellationToken = default);

    Task<AccountRegistration?> AddAccountAsync(
        string id,
        string password,
        string fullName,
        string email,
        CancellationToken cancellationToken = default);

    Task<AccountLogin?> LoginAsync(
        string id,
        string password,
        CancellationToken cancellationToken = default);

    Task<bool> VerifyEmailAsync(
        string sub,
        string verifyCode,
        CancellationToken cancellationToken = default);

    Task<EmailVerificationDelivery?> RefreshEmailVerificationAsync(
        string sub,
        CancellationToken cancellationToken = default);
}
