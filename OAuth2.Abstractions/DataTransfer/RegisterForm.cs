using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace OAuth2.DataTransfer;

public partial record RegisterForm
{
    public required string Id { get; init; }

    public required string Password { get; init; }

    public required string FullName { get; init; }

    public required string Email { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(Password) || string.IsNullOrEmpty(FullName) || string.IsNullOrEmpty(Email))
        {
            error = "body.id is missing";
            return false;
        }

        if (string.IsNullOrEmpty(Password))
        {
            error = "body.password is missing";
            return false;
        }

        if (string.IsNullOrEmpty(FullName))
        {
            error = "body.fullName is missing";
            return false;
        }

        if (string.IsNullOrEmpty(Email))
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

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase, "ko-KR")]
    private static partial Regex ValidEmailRegex();
}
