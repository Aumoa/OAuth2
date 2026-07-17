using System.Diagnostics.CodeAnalysis;

namespace OAuth2.DataTransfer;

public sealed record TransferOrganizationOwnershipForm
{
    public required string AccountId { get; init; }

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

        error = null;
        return true;
    }
}
