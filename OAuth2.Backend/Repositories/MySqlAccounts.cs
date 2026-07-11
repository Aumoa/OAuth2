using System.Security.Cryptography;
using Dapper;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using OAuth2.Data;
using OAuth2.Options;
using OAuth2.Services;

namespace OAuth2.Repositories;

internal class MySqlAccounts(IOptions<MySqlOptions> options, PasswordHasher hasher) : IAccounts
{
    public async Task<Account?> GetAccountAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        using var connection = new MySqlConnection(options.Value.ConnectionString);

        const string QUERY = "SELECT `id`, `password`, `sub`, `name`, `email`, `verify_code` AS `VerifyCode`, `created_at` AS `CreatedAt` FROM `account` WHERE `id` = @id";
        var command = new CommandDefinition(QUERY, new { id }, cancellationToken: cancellationToken);

        await connection.OpenAsync(cancellationToken);
        var account = await connection.QueryFirstOrDefaultAsync<Account>(command);
        return account;
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

        var verifyCode = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var encryptedVerifyCode = hasher.Hash(verifyCode);
        var sub = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var passwordHash = hasher.Hash(password);

        using var connection = new MySqlConnection(options.Value.ConnectionString);

        const string QUERY = "INSERT INTO `account` (`id`, `password`, `sub`, `name`, `email`, `verify_code`) VALUES(@id, @password, @sub, @name, @email, @verifyCode)";
        var command = new CommandDefinition(
            QUERY,
            new
            {
                id,
                password = passwordHash,
                sub,
                name = fullName,
                email,
                verifyCode = encryptedVerifyCode
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
}
