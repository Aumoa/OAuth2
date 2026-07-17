using System.Diagnostics.CodeAnalysis;
using OAuth2.Data;

namespace OAuth2.DataTransfer;

public sealed record UpdateOrganizationMemberForm
{
    public required string Role { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (!OrganizationRoles.IsAssignable(Role))
        {
            error = "body.role must be admin or member";
            return false;
        }

        error = null;
        return true;
    }
}
