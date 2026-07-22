using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Data;
using OAuth2.DataTransfer;
using OAuth2.Options;

namespace OAuth2.Repositories;

internal sealed class MySqlAccountProfiles(IOptions<MySqlOptions> mysqlOptions) : IAccountProfiles
{
    private sealed record AccountProfileRow(string FullName, DateTime UpdatedAt);

    public async Task<AccountProfile?> GetAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var account = await connection.QuerySingleOrDefaultAsync<AccountProfileRow>(
            new CommandDefinition(
                "SELECT `name` AS `FullName`, `updated_at` AS `UpdatedAt` FROM `account` WHERE `id` = @accountId",
                new { accountId },
                cancellationToken: cancellationToken));
        if (account is null)
        {
            return null;
        }

        var claims = await GetClaimsAsync(connection, accountId, null, cancellationToken);
        return CreateProfile(account.FullName, account.UpdatedAt, claims);
    }

    public async Task<AccountProfile?> UpdateAsync(
        string accountId,
        UpdateAccountProfileForm form,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentNullException.ThrowIfNull(form);
        if (!form.Verify(out _))
        {
            throw new ArgumentException("Form verification failed.", nameof(form));
        }

        var fullName = form.FullName.Trim();
        var claimValues = form.Claims
            .Select(static claim => new AccountProfileClaim
            {
                Name = claim.Name,
                Value = claim.Value.Trim()
            })
            .ToList();
        if (!string.IsNullOrWhiteSpace(form.Nickname))
        {
            claimValues.Insert(0, new AccountProfileClaim
            {
                Name = AccountProfileClaimTypes.Nickname,
                Value = form.Nickname.Trim()
            });
        }

        var updatedAt = DateTime.UtcNow;
        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var exists = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(
                "SELECT `id` FROM `account` WHERE `id` = @accountId FOR UPDATE",
                new { accountId },
                transaction,
                cancellationToken: cancellationToken));
        if (exists is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE `account` SET `name` = @fullName, `updated_at` = @updatedAt WHERE `id` = @accountId",
            new { accountId, fullName, updatedAt },
            transaction,
            cancellationToken: cancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(
            "UPDATE `account_claim` SET `removed_at` = @updatedAt WHERE `account_id` = @accountId AND `name` IN @claimNames AND `removed_at` IS NULL",
            new
            {
                accountId,
                updatedAt,
                claimNames = AccountProfileClaimTypes.Editable
            },
            transaction,
            cancellationToken: cancellationToken));

        foreach (var claim in claimValues)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO `account_claim` (`account_id`, `name`, `value`, `created_at`) VALUES (@accountId, @name, @value, @updatedAt)",
                new { accountId, claim.Name, claim.Value, updatedAt },
                transaction,
                cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
        return CreateProfile(fullName, updatedAt, claimValues);
    }

    private static async Task<IReadOnlyList<AccountProfileClaim>> GetClaimsAsync(
        MySqlConnection connection,
        string accountId,
        MySqlTransaction? transaction,
        CancellationToken cancellationToken)
    {
        var claims = await connection.QueryAsync<AccountProfileClaim>(new CommandDefinition(
            "SELECT `name`, `value` FROM `account_claim` WHERE `account_id` = @accountId AND `name` IN @claimNames AND `removed_at` IS NULL ORDER BY `created_at`, `id`",
            new
            {
                accountId,
                claimNames = AccountProfileClaimTypes.Editable
            },
            transaction,
            cancellationToken: cancellationToken));
        return claims
            .GroupBy(static claim => claim.Name, StringComparer.Ordinal)
            .Select(static group => group.Last())
            .ToArray();
    }

    private static AccountProfile CreateProfile(
        string fullName,
        DateTime updatedAt,
        IReadOnlyList<AccountProfileClaim> savedClaims)
    {
        var claims = savedClaims.ToDictionary(static claim => claim.Name, StringComparer.Ordinal);
        return new AccountProfile
        {
            FullName = fullName,
            Nickname = claims.GetValueOrDefault(AccountProfileClaimTypes.Nickname)?.Value,
            Claims = AccountProfileClaimTypes.Additional
                .Where(claims.ContainsKey)
                .Select(name => claims[name])
                .ToArray(),
            UpdatedAt = new DateTimeOffset(
                DateTime.SpecifyKind(updatedAt, DateTimeKind.Utc)).ToUnixTimeSeconds()
        };
    }
}
