using System.Diagnostics.CodeAnalysis;
using OAuth2.Data;

namespace OAuth2.DataTransfer;

public sealed record AddOrganizationMemberForm
{
    public required string AccountId { get; init; }

    public required string Role { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(AccountId))
        {
            error = "body.accountId is missing";
            return false;
        }

        if (AccountId.Trim().Length > 128)
        {
            error = "body.accountId exceeds 128 characters";
            return false;
        }

        if (!OrganizationRoles.IsAssignable(Role))
        {
            error = "body.role must be admin or member";
            return false;
        }

        error = null;
        return true;
    }
}
