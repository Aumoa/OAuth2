using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace OAuth2.DataTransfer;

public sealed partial record CreateOrganizationGroupForm
{
    public const int IdMaxLength = 64;

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

        if (!GroupIdPattern().IsMatch(id))
        {
            error = "body.id must contain only lowercase letters, numbers, or single hyphens";
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

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex GroupIdPattern();
}
