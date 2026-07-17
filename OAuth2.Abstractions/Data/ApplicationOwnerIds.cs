using System.Security.Cryptography;
using System.Text;

namespace OAuth2.Data;

public static class ApplicationOwnerIds
{
    public const string OrganizationPrefix = "organization:";

    public static string CreateForOrganization(string organizationId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(organizationId);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(organizationId));
        return $"{OrganizationPrefix}{Convert.ToHexStringLower(hash)}";
    }

    public static bool IsReservedAccountId(string? accountId) =>
        accountId?.StartsWith(OrganizationPrefix, StringComparison.OrdinalIgnoreCase) == true;
}
