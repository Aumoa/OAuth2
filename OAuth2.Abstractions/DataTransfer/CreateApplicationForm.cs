using System.Diagnostics.CodeAnalysis;

namespace OAuth2.DataTransfer;

public sealed record CreateApplicationForm
{
    public const int ClientIdMaxLength = 128;

    public const int NameMaxLength = 512;

    public required string ClientId { get; init; }

    public required string Name { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(ClientId))
        {
            error = "body.clientId is missing";
            return false;
        }

        if (ClientId.Trim().Length > ClientIdMaxLength)
        {
            error = $"body.clientId exceeds {ClientIdMaxLength} characters";
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
}
