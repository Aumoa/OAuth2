using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Options;

namespace OAuth2.Repositories;

internal sealed class MySqlAccountClaims(IOptions<MySqlOptions> mysqlOptions) : IAccountClaims
{
    public async Task<IReadOnlyList<AccountClaim>> GetClaimsAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        const string QUERY = "SELECT `name`, `value`, `created_at` AS `CreatedAt` FROM `account_claim` WHERE `account_id` = @accountId AND `removed_at` IS NULL ORDER BY `created_at`, `id`";
        var command = new CommandDefinition(
            QUERY,
            new { accountId },
            cancellationToken: cancellationToken);

        await connection.OpenAsync(cancellationToken);
        var claims = await connection.QueryAsync<AccountClaim>(command);
        return claims
            .GroupBy(claim => claim.Name, StringComparer.Ordinal)
            .Select(group => group.Last())
            .ToArray();
    }
}
