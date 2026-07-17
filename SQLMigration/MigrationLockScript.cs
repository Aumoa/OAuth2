using System.Security.Cryptography;
using System.Text;
using Dapper;
using MySql.Data.MySqlClient;

namespace SQLMigration;

internal static class MigrationLockScript
{
    private const int LockTimeoutSeconds = 300;

    public static async ValueTask<string> EnterAsync(
        MySqlConnection connection,
        string databaseName,
        TextWriter logger,
        CancellationToken cancellationToken = default)
    {
        var lockName = CreateLockName(databaseName);
        logger.WriteLine("Waiting to acquire the database migration lock...");

        const string QUERY = "SELECT GET_LOCK(@lockName, @timeoutSeconds)";
        var command = new CommandDefinition(
            QUERY,
            new { lockName, timeoutSeconds = LockTimeoutSeconds },
            commandTimeout: LockTimeoutSeconds + 5,
            cancellationToken: cancellationToken);
        var acquired = await connection.ExecuteScalarAsync<int?>(command);
        if (acquired == 1)
        {
            logger.WriteLine("Database migration lock acquired.");
            return lockName;
        }

        if (acquired == 0)
        {
            throw new TimeoutException(
                $"Could not acquire the database migration lock within {LockTimeoutSeconds} seconds.");
        }

        throw new InvalidOperationException("MySQL could not acquire the database migration lock.");
    }

    public static async ValueTask LeaveAsync(
        MySqlConnection connection,
        string lockName,
        TextWriter logger,
        CancellationToken cancellationToken = default)
    {
        const string QUERY = "SELECT RELEASE_LOCK(@lockName)";
        var command = new CommandDefinition(
            QUERY,
            new { lockName },
            commandTimeout: 5,
            cancellationToken: cancellationToken);
        var released = await connection.ExecuteScalarAsync<int?>(command);
        if (released == 1)
        {
            logger.WriteLine("Database migration lock released.");
            return;
        }

        logger.WriteLine("The database migration lock was no longer owned by this connection.");
    }

    private static string CreateLockName(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var readableName = $"SQLMigration:{databaseName}";
        if (readableName.Length <= 64)
        {
            return readableName;
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(databaseName));
        return $"SQLMigration:{Convert.ToHexString(hash.AsSpan(0, 25))}";
    }
}
