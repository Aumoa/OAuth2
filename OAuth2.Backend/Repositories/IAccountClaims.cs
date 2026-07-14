namespace OAuth2.Repositories;

public interface IAccountClaims
{
    Task<IReadOnlyList<AccountClaim>> GetClaimsAsync(
        string accountId,
        CancellationToken cancellationToken = default);
}
