using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Data;
using OAuth2.Options;

namespace OAuth2.Repositories;

internal sealed class MySqlApplications(IOptions<MySqlOptions> mysqlOptions) : IApplications
{
    public async Task<IReadOnlyList<OAuthApplication>> GetOwnedApplicationsAsync(
        string ownerId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ownerId);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);

        const string QUERY = """
            SELECT
                `id`,
                `owner_id` AS `OwnerId`,
                `name`,
                `created_at` AS `CreatedAt`
            FROM `client`
            WHERE `owner_id` = @ownerId
                AND `removed_at` IS NULL
            ORDER BY `created_at` DESC, `id`
            """;
        var command = new CommandDefinition(
            QUERY,
            new { ownerId },
            cancellationToken: cancellationToken);
        var applications = await connection.QueryAsync<OAuthApplication>(command);
        return applications.ToArray();
    }
}
