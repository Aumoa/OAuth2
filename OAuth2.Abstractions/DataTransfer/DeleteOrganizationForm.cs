using System.Diagnostics.CodeAnalysis;

namespace OAuth2.DataTransfer;

public sealed record DeleteOrganizationForm
{
    public required string Name { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            error = "body.name is missing";
            return false;
        }

        if (Name.Length > 128)
        {
            error = "body.name exceeds 128 characters";
            return false;
        }

        error = null;
        return true;
    }
}
