using System.Diagnostics.CodeAnalysis;

namespace OAuth2.DataTransfer;

public sealed record AddApplicationRoleMemberForm
{
    public const int AccountIdentifierMaxLength = 128;

    public required string AccountId { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        var accountId = AccountId?.Trim();
        if (string.IsNullOrWhiteSpace(accountId))
        {
            error = "body.accountId is missing";
            return false;
        }

        if (accountId.Length > AccountIdentifierMaxLength)
        {
            error = $"body.accountId exceeds {AccountIdentifierMaxLength} characters";
            return false;
        }

        error = null;
        return true;
    }
}
