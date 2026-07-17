using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using OAuth2.Data;

namespace OAuth2.DataTransfer;

public partial record RegisterForm
{
    public required string Id { get; init; }

    public required string Password { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            error = "body.id is missing";
            return false;
        }

        if (ApplicationOwnerIds.IsReservedAccountId(Id.Trim()))
        {
            error = "body.id is reserved";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            error = "body.password is missing";
            return false;
        }

        if (string.IsNullOrWhiteSpace(FullName))
        {
            error = "body.fullName is missing";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Email))
        {
            error = "body.email is missing";
            return false;
        }

        if (!ValidEmailRegex().IsMatch(Email))
        {
            error = "body.email is not valid format";
            return false;
        }

        error = null;
        return true;
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex ValidEmailRegex();
}
