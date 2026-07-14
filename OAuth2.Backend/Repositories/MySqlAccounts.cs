using System.Security.Cryptography;
using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Data;
using OAuth2.Options;
using OAuth2.Services;

namespace OAuth2.Repositories;

internal class MySqlAccounts(
    IOptions<MySqlOptions> mysqlOptions,
    IOptions<SESOptions> sesOptions,
    PasswordHasher hasher) : IAccounts
{
    private sealed class LoginRow
    {
        public string Password { get; init; } = string.Empty;

        public string Sub { get; init; } = string.Empty;

        public string? VerifyCode { get; init; }
    }

    private sealed class EmailVerificationRow
    {
        public string? VerifyCode { get; init; }

        public DateTime? VerifyCodeExpiresAt { get; init; }
    }

    public async Task<Account?> GetAccountAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);

        const string QUERY = "SELECT `id`, `password`, `sub`, `name`, `email`, `verify_code` AS `VerifyCode`, `verify_code_expires_at` AS `VerifyCodeExpiresAt`, `created_at` AS `CreatedAt`, `updated_at` AS `UpdatedAt` FROM `account` WHERE `id` = @id";
        var command = new CommandDefinition(QUERY, new { id }, cancellationToken: cancellationToken);

        await connection.OpenAsync(cancellationToken);
        return await connection.QueryFirstOrDefaultAsync<Account>(command);
    }

    public async Task<AccountRegistration?> AddAccountAsync(
        string id,
        string password,
        string fullName,
        string email,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(fullName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);

        var verifyCode = CreateVerifyCode();
        var encryptedVerifyCode = hasher.Hash(verifyCode);
        var verifyCodeExpiresAt = CreateExpiration();
        var sub = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var passwordHash = hasher.Hash(password);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);

        const string QUERY = "INSERT INTO `account` (`id`, `password`, `sub`, `name`, `email`, `verify_code`, `verify_code_expires_at`) VALUES(@id, @password, @sub, @name, @email, @verifyCode, @verifyCodeExpiresAt)";
        var command = new CommandDefinition(
            QUERY,
            new
            {
                id,
                password = passwordHash,
                sub,
                name = fullName,
                email,
                verifyCode = encryptedVerifyCode,
                verifyCodeExpiresAt
            },
            cancellationToken: cancellationToken);

        try
        {
            var affectedRows = await connection.ExecuteAsync(command);
            if (affectedRows != 1)
            {
                return null;
            }
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            return null;
        }

        return new AccountRegistration(sub, verifyCode);
    }

    public async Task<AccountLogin?> LoginAsync(
        string id,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);

        const string QUERY = "SELECT `password`, `sub`, `verify_code` AS `VerifyCode` FROM `account` WHERE `id` = @id";
        var command = new CommandDefinition(QUERY, new { id }, cancellationToken: cancellationToken);
        var account = await connection.QuerySingleOrDefaultAsync<LoginRow>(command);
        if (account is null || !hasher.Verify(password, account.Password))
        {
            return null;
        }

        return new AccountLogin(id, account.Sub, string.IsNullOrEmpty(account.VerifyCode));
    }

    public async Task<bool> VerifyEmailAsync(
        string sub,
        string verifyCode,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sub);
        ArgumentException.ThrowIfNullOrWhiteSpace(verifyCode);

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string SELECT_QUERY = "SELECT `verify_code` AS `VerifyCode`, `verify_code_expires_at` AS `VerifyCodeExpiresAt` FROM `account` WHERE `sub` = @sub FOR UPDATE";
        var command = new CommandDefinition(
            SELECT_QUERY,
            new { sub },
            transaction,
            cancellationToken: cancellationToken);
        var verification = await connection.QuerySingleOrDefaultAsync<EmailVerificationRow>(command);
        if (verification is null)
        {
            return false;
        }

        if (verification.VerifyCode is null)
        {
            return true;
        }

        if (verification.VerifyCodeExpiresAt is null
            || verification.VerifyCodeExpiresAt <= DateTime.UtcNow
            || !hasher.Verify(verifyCode, verification.VerifyCode))
        {
            return false;
        }

        const string UPDATE_QUERY = "UPDATE `account` SET `verify_code` = NULL, `verify_code_expires_at` = NULL WHERE `sub` = @sub AND `verify_code` = @savedVerifyCode";
        command = new CommandDefinition(
            UPDATE_QUERY,
            new { sub, savedVerifyCode = verification.VerifyCode },
            transaction,
            cancellationToken: cancellationToken);
        var affectedRows = await connection.ExecuteAsync(command);
        if (affectedRows != 1)
        {
            return false;
        }

        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<EmailVerificationDelivery?> RefreshEmailVerificationAsync(
        string sub,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sub);

        var verifyCode = CreateVerifyCode();
        var encryptedVerifyCode = hasher.Hash(verifyCode);
        var verifyCodeExpiresAt = CreateExpiration();

        using var connection = new MySqlConnection(mysqlOptions.Value.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        const string UPDATE_QUERY = "UPDATE `account` SET `verify_code` = @verifyCode, `verify_code_expires_at` = @verifyCodeExpiresAt WHERE `sub` = @sub AND `verify_code` IS NOT NULL";
        var command = new CommandDefinition(
            UPDATE_QUERY,
            new { sub, verifyCode = encryptedVerifyCode, verifyCodeExpiresAt },
            transaction,
            cancellationToken: cancellationToken);
        if (await connection.ExecuteAsync(command) != 1)
        {
            return null;
        }

        const string SELECT_QUERY = "SELECT `email` FROM `account` WHERE `sub` = @sub";
        command = new CommandDefinition(
            SELECT_QUERY,
            new { sub },
            transaction,
            cancellationToken: cancellationToken);
        var email = await connection.QuerySingleAsync<string>(command);

        await transaction.CommitAsync(cancellationToken);
        return new EmailVerificationDelivery(sub, verifyCode, email);
    }

    private string CreateVerifyCode() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    private DateTime CreateExpiration() =>
        DateTime.UtcNow.AddMinutes(sesOptions.Value.VerificationCodeValidMinutes);
}
