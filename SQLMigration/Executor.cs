using System.Text;
using Dapper;
using MySql.Data.MySqlClient;

namespace SQLMigration;

public static class Executor
{
    public static async ValueTask RunAsync(
        string connectionString,
        string databaseName,
        IScript[] scripts,
        TextWriter logger,
        AppliedMigrationMismatchBehavior mismatchBehavior,
        CancellationToken cancellationToken = default)
    {
        if (mismatchBehavior is not AppliedMigrationMismatchBehavior.Fail
            and not AppliedMigrationMismatchBehavior.RevertAndApply)
        {
            throw new ArgumentOutOfRangeException(nameof(mismatchBehavior));
        }

        var builder = new MySqlConnectionStringBuilder(connectionString)
        {
            Database = string.Empty
        };
        using var connection = new MySqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);

        var escapedDatabaseName = databaseName.Replace("`", "``");
        string QUERY1 = @$"
CREATE SCHEMA IF NOT EXISTS `{escapedDatabaseName}`;
USE `{escapedDatabaseName}`;
";

        var commandDef = new CommandDefinition(QUERY1, cancellationToken: cancellationToken);
        await connection.ExecuteAsync(commandDef);

        var lockName = await MigrationLockScript.EnterAsync(connection, databaseName, logger, cancellationToken);
        try
        {
            var installedItems = await MigrationTableScript.ReadAsync(connection, cancellationToken);
            StringBuilder? sb = null;
            foreach (var installed in installedItems)
            {
                if (installed.Status is ExecutionStatus.UpFailed or ExecutionStatus.DownFailed)
                {
                    sb ??= new StringBuilder();
                    sb.AppendLine($"Migration '{installed.Name}' with rank {installed.InstalledRank} has failed with status '{installed.Status}'.");
                }
            }

            if (sb != null)
            {
                throw new InvalidOperationException(sb.ToString() + "One or more failed migration items exist. You must resolve these migration failures manually. After resolving, you must also update the values in the __MigrationHistory table accordingly.");
            }

            scripts = [.. scripts.OrderBy(s => s.InstalledRank)];
            var scriptsByRank = scripts.ToDictionary(static script => script.InstalledRank);
            var installedByRank = installedItems.ToDictionary(static installed => installed.InstalledRank);
            var divergenceRank = int.MaxValue;
            foreach (var script in scripts)
            {
                if (!installedByRank.TryGetValue(script.InstalledRank, out var installed)
                    || installed.UpSql != script.UpSql
                    || installed.DownSql != script.DownSql)
                {
                    divergenceRank = Math.Min(divergenceRank, script.InstalledRank);
                }
            }

            foreach (var installed in installedItems)
            {
                if (!scriptsByRank.ContainsKey(installed.InstalledRank))
                {
                    divergenceRank = Math.Min(divergenceRank, installed.InstalledRank);
                }
            }

            if (divergenceRank == int.MaxValue)
            {
                logger.WriteLine("No new migrations to apply.");
                return;
            }

            var migrationsToRevert = installedItems
                .Where(installed => installed.InstalledRank >= divergenceRank)
                .OrderByDescending(static installed => installed.InstalledRank)
                .ToArray();
            if (migrationsToRevert.Length > 0
                && mismatchBehavior == AppliedMigrationMismatchBehavior.Fail)
            {
                throw new InvalidOperationException(
                    $"Applied migration history diverges at rank {divergenceRank}. "
                    + "Automatic rollback is disabled in this environment. "
                    + "Restore the applied migration scripts or add a new forward-only migration.");
            }

            foreach (var installed in migrationsToRevert)
            {
                logger.WriteLine("Reverting migration with rank {0} - {1} to the previous state.", installed.InstalledRank, installed.Name);

                try
                {
                    await using var tx = await connection.BeginTransactionAsync(cancellationToken);
                    commandDef = new CommandDefinition(installed.DownSql, transaction: tx, cancellationToken: cancellationToken);
                    await connection.ExecuteAsync(commandDef);
                    await MigrationTableScript.DownAsync(connection, installed, cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                catch (Exception)
                {
                    await MigrationTableScript.DownFailedAsync(connection, installed, cancellationToken);
                    throw;
                }
            }

            foreach (var script in scripts.Where(script => script.InstalledRank >= divergenceRank))
            {
                logger.WriteLine("Applying migration with rank {0} - {1}.", script.InstalledRank, script.Name);

                try
                {
                    await using var tx = await connection.BeginTransactionAsync(cancellationToken);
                    commandDef = new CommandDefinition(script.UpSql, transaction: tx, cancellationToken: cancellationToken);
                    await connection.ExecuteAsync(commandDef);
                    await MigrationTableScript.UpAsync(connection, script, cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                }
                catch (Exception)
                {
                    await MigrationTableScript.FailedAsync(connection, script, cancellationToken);
                    throw;
                }
            }

            logger.WriteLine("Migration completed successfully.");
        }
        finally
        {
            await MigrationLockScript.LeaveAsync(connection, lockName, logger, CancellationToken.None);
        }
    }
}
