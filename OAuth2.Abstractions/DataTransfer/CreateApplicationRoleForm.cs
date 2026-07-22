using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace OAuth2.DataTransfer;

public sealed partial record CreateApplicationRoleForm
{
    public const int IdMaxLength = 128;

    public const int NameMaxLength = 128;

    public required string Id { get; init; }

    public required string Name { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        var id = Id?.Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            error = "body.id is missing";
            return false;
        }

        if (id.Length > IdMaxLength)
        {
            error = $"body.id exceeds {IdMaxLength} characters";
            return false;
        }

        if (!RoleIdPattern().IsMatch(id))
        {
            error = "body.id contains unsupported characters";
            return false;
        }

        var name = Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "body.name is missing";
            return false;
        }

        if (name.Length > NameMaxLength)
        {
            error = $"body.name exceeds {NameMaxLength} characters";
            return false;
        }

        error = null;
        return true;
    }

    [GeneratedRegex("^[a-z0-9]+(?:[._:-][a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex RoleIdPattern();
}
