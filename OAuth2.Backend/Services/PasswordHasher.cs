using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using OAuth2.Options;

namespace OAuth2.Services;

public class PasswordHasher(IOptions<PasswordHasherOptions> options)
{
    private const int SaltSize = 32;
    private const int HashSize = 32;

    public string Hash(string password)
    {
        var iterations = options.Value.Iterations;
        byte[] saltBytes = RandomNumberGenerator.GetBytes(SaltSize);
        var salt = Convert.ToBase64String(saltBytes);

        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            password,
            saltBytes,
            iterations,
            HashAlgorithmName.SHA256,
            HashSize);
        var hash = Convert.ToBase64String(hashBytes);

        return $"{hash}${salt}${iterations}";
    }

    public bool Verify(string password, string saved)
    {
        var components = saved.Split('$', 3);
        if (components.Length != 3)
        {
            return false;
        }

        var hash = components[0];
        var salt = components[1];

        if (!int.TryParse(components[2], out var iterations) || iterations <= 0)
        {
            return false;
        }

        try
        {
            byte[] saltBytes = Convert.FromBase64String(salt);
            byte[] computedHashBytes = Rfc2898DeriveBytes.Pbkdf2(
                password,
                saltBytes,
                iterations,
                HashAlgorithmName.SHA256,
                HashSize);
            byte[] savedHashBytes = Convert.FromBase64String(hash);
            return CryptographicOperations.FixedTimeEquals(computedHashBytes, savedHashBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
