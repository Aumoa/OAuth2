using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Data;
using OAuth2.Options;

namespace OAuth2.Repositories;

internal sealed class MySqlAccountProfileImages(
    IOptions<MySqlOptions> mysqlOptions) : IAccountProfileImages
{
    public async Task<AccountProfileImage?> GetAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        const string QUERY = "SELECT `data`, `content_type` AS `ContentType`, `width`, `height`, `hash` AS `Hash`, `updated_at` AS `UpdatedAt` FROM `account_profile_image` WHERE `account_id` = @accountId";
        var command = new CommandDefinition(
            QUERY,
            new { accountId },
            cancellationToken: cancellationToken);
        return await connection.QuerySingleOrDefaultAsync<AccountProfileImage>(command);
    }

    public async Task<bool> UpsertAsync(
        string accountId,
        ProcessedProfileImage image,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);
        ArgumentNullException.ThrowIfNull(image);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string UPDATE_ACCOUNT_QUERY = "UPDATE `account` SET `updated_at` = UTC_TIMESTAMP(6) WHERE `id` = @accountId";
        var command = new CommandDefinition(
            UPDATE_ACCOUNT_QUERY,
            new { accountId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            return false;
        }

        const string UPSERT_QUERY = @"
INSERT INTO `account_profile_image`
    (`account_id`, `content_type`, `data`, `width`, `height`, `hash`, `updated_at`)
VALUES (@accountId, @contentType, @data, @width, @height, @hash, UTC_TIMESTAMP(6))
ON DUPLICATE KEY UPDATE
    `content_type` = VALUES(`content_type`),
    `data` = VALUES(`data`),
    `width` = VALUES(`width`),
    `height` = VALUES(`height`),
    `hash` = VALUES(`hash`),
    `updated_at` = VALUES(`updated_at`);";
        var parameters = new
        {
            accountId,
            contentType = ProfileImagePolicy.StoredContentType,
            image.Data,
            image.Width,
            image.Height,
            image.Hash
        };
        command = new CommandDefinition(
            UPSERT_QUERY,
            parameters,
            transaction,
            cancellationToken: cancellationToken);
        _ = await connection.ExecuteAsync(command);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string UPDATE_ACCOUNT_QUERY = "UPDATE `account` SET `updated_at` = UTC_TIMESTAMP() WHERE `id` = @accountId";
        var command = new CommandDefinition(
            UPDATE_ACCOUNT_QUERY,
            new { accountId },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            return false;
        }

        const string DELETE_QUERY = "DELETE FROM `account_profile_image` WHERE `account_id` = @accountId";
        command = new CommandDefinition(
            DELETE_QUERY,
            new { accountId },
            transaction,
            cancellationToken: cancellationToken);
        _ = await connection.ExecuteAsync(command);

        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
