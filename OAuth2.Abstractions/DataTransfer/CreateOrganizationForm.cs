using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace OAuth2.DataTransfer;

public sealed partial record CreateOrganizationForm
{
    public const int IdMaxLength = 64;

    public const int NameMaxLength = 128;

    public required string Id { get; init; }

    public required string Name { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            error = "body.id is missing";
            return false;
        }

        var normalizedId = Id.Trim();
        if (normalizedId.Length > IdMaxLength)
        {
            error = $"body.id exceeds {IdMaxLength} characters";
            return false;
        }

        if (!OrganizationIdPattern().IsMatch(normalizedId))
        {
            error = "body.id must contain only lowercase letters, numbers, or single hyphens";
            return false;
        }

        if (string.Equals(normalizedId, "new", StringComparison.Ordinal))
        {
            error = "body.id is reserved";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Name))
        {
            error = "body.name is missing";
            return false;
        }

        if (Name.Trim().Length > NameMaxLength)
        {
            error = $"body.name exceeds {NameMaxLength} characters";
            return false;
        }

        error = null;
        return true;
    }

    [GeneratedRegex("^[a-z0-9]+(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex OrganizationIdPattern();
}
