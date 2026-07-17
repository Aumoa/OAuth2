using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.OpenId;

namespace OAuth2.Services;

public sealed class OidcSigningKey : IDisposable
{
    private readonly RSA m_Key;
    private readonly object m_Sync = new();

    public OidcSigningKey(
        IOptions<OidcProviderOptions> options,
        IWebHostEnvironment environment,
        ILogger<OidcSigningKey> logger)
    {
        var path = options.Value.SigningKeyPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            if (!environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "OpenId:SigningKeyPath is required outside Development.");
            }

            logger.LogWarning(
                "OpenId:SigningKeyPath is not configured. Using an ephemeral development signing key.");
            m_Key = RSA.Create(2048);
        }
        else
        {
            var resolvedPath = Path.IsPathRooted(path)
                ? path
                : Path.Combine(environment.ContentRootPath, path);
            m_Key = RSA.Create();
            m_Key.ImportFromPem(File.ReadAllText(resolvedPath));
            if (m_Key.KeySize < 2048)
            {
                m_Key.Dispose();
                throw new InvalidOperationException(
                    "The OIDC RSA signing key must be at least 2048 bits.");
            }
        }

        KeyId = OidcJwt.Base64UrlEncode(
            SHA256.HashData(m_Key.ExportSubjectPublicKeyInfo()));
    }

    public string KeyId { get; }

    public string CreateToken(
        string type,
        IReadOnlyDictionary<string, object?> claims)
    {
        lock (m_Sync)
        {
            return OidcJwt.Create(m_Key, KeyId, type, claims);
        }
    }

    public bool TryValidate(
        string? token,
        string expectedType,
        string expectedIssuer,
        DateTimeOffset now,
        out Dictionary<string, System.Text.Json.JsonElement>? claims)
    {
        lock (m_Sync)
        {
            return OidcJwt.TryValidate(
                token,
                m_Key,
                KeyId,
                expectedType,
                expectedIssuer,
                now,
                out claims);
        }
    }

    public OidcJsonWebKeySet GetJsonWebKeySet()
    {
        RSAParameters parameters;
        lock (m_Sync)
        {
            parameters = m_Key.ExportParameters(false);
        }

        return new OidcJsonWebKeySet
        {
            Keys =
            [
                new OidcJsonWebKey
                {
                    KeyId = KeyId,
                    Modulus = OidcJwt.Base64UrlEncode(parameters.Modulus!),
                    Exponent = OidcJwt.Base64UrlEncode(parameters.Exponent!)
                }
            ]
        };
    }

    public void Dispose()
    {
        m_Key.Dispose();
    }
}
