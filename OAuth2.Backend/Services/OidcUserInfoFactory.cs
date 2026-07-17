using System.Globalization;
using System.Text.Json;
using OAuth2.Data;
using OAuth2.OpenId;
using OAuth2.Repositories;

namespace OAuth2.Services;

internal static class OidcUserInfoFactory
{
    public static Dictionary<string, JsonElement> Create(
        Account account,
        IReadOnlyList<AccountClaim> accountClaims,
        IReadOnlyList<OrganizationClaimValue> organizationClaims,
        string scope)
    {
        ArgumentNullException.ThrowIfNull(account);
        ArgumentNullException.ThrowIfNull(accountClaims);
        ArgumentNullException.ThrowIfNull(organizationClaims);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        var allowedClaimNames = OidcClaimPolicy.GetAllowedClaimNames(scope);
        var claims = new Dictionary<string, JsonElement>(StringComparer.Ordinal);

        foreach (var accountClaim in accountClaims)
        {
            if (allowedClaimNames.Contains(accountClaim.Name)
                && TryCreateClaimValue(accountClaim.Name, accountClaim.Value, out var value))
            {
                claims[accountClaim.Name] = value;
            }
        }

        AddStringClaim(claims, allowedClaimNames, "sub", account.Sub);
        AddStringClaim(claims, allowedClaimNames, "name", account.Name);
        AddStringClaim(claims, allowedClaimNames, "preferred_username", account.Id);
        AddStringClaim(claims, allowedClaimNames, "email", account.Email);

        if (allowedClaimNames.Contains("email_verified"))
        {
            claims["email_verified"] = JsonSerializer.SerializeToElement(
                string.IsNullOrEmpty(account.VerifyCode));
        }

        var updatedAt = account.UpdatedAt ?? account.CreatedAt;
        if (allowedClaimNames.Contains("updated_at") && updatedAt.HasValue)
        {
            var utcUpdatedAt = DateTime.SpecifyKind(updatedAt.Value, DateTimeKind.Utc);
            claims["updated_at"] = JsonSerializer.SerializeToElement(
                new DateTimeOffset(utcUpdatedAt).ToUnixTimeSeconds());
        }

        AddOrganizationClaims(claims, allowedClaimNames, organizationClaims);

        return claims;
    }

    private static void AddOrganizationClaims(
        IDictionary<string, JsonElement> claims,
        IReadOnlySet<string> allowedClaimNames,
        IReadOnlyList<OrganizationClaimValue> organizationClaims)
    {
        if (allowedClaimNames.Contains("groups"))
        {
            var groups = new SortedSet<string>(StringComparer.Ordinal);
            if (claims.TryGetValue("groups", out var savedGroups)
                && savedGroups.ValueKind == JsonValueKind.Array)
            {
                foreach (var savedGroup in savedGroups.EnumerateArray())
                {
                    if (savedGroup.ValueKind == JsonValueKind.String
                        && savedGroup.GetString() is { Length: > 0 } value)
                    {
                        groups.Add(value);
                    }
                }
            }

            foreach (var organization in organizationClaims)
            {
                groups.Add(organization.Id);
                foreach (var groupId in organization.GroupIds)
                {
                    groups.Add($"{organization.Id}-{groupId}");
                }
            }

            claims["groups"] = JsonSerializer.SerializeToElement(groups.ToArray());
        }

        if (allowedClaimNames.Contains("organization"))
        {
            claims["organization"] = JsonSerializer.SerializeToElement(
                organizationClaims.Select(static claim => new
                {
                    id = claim.Id,
                    name = claim.Name,
                    role = claim.Role
                }).ToArray());
        }
    }

    private static void AddStringClaim(
        IDictionary<string, JsonElement> claims,
        IReadOnlySet<string> allowedClaimNames,
        string name,
        string? value)
    {
        if (allowedClaimNames.Contains(name) && !string.IsNullOrWhiteSpace(value))
        {
            claims[name] = JsonSerializer.SerializeToElement(value);
        }
    }

    private static bool TryCreateClaimValue(
        string name,
        string value,
        out JsonElement claimValue)
    {
        claimValue = default;

        if (name is "email_verified" or "phone_number_verified")
        {
            if (!bool.TryParse(value, out var booleanValue))
            {
                return false;
            }

            claimValue = JsonSerializer.SerializeToElement(booleanValue);
            return true;
        }

        if (name == "updated_at")
        {
            if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var integerValue))
            {
                return false;
            }

            claimValue = JsonSerializer.SerializeToElement(integerValue);
            return true;
        }

        if (name is "address" or "groups")
        {
            try
            {
                using var document = JsonDocument.Parse(value);
                var expectedKind = name == "address" ? JsonValueKind.Object : JsonValueKind.Array;
                if (document.RootElement.ValueKind != expectedKind)
                {
                    return false;
                }

                claimValue = document.RootElement.Clone();
                return true;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        claimValue = JsonSerializer.SerializeToElement(value);
        return true;
    }
}
