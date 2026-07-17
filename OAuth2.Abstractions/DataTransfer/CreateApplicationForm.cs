using System.Diagnostics.CodeAnalysis;
using OAuth2.Data;

namespace OAuth2.DataTransfer;

public sealed record CreateApplicationForm
{
    public const int ClientIdMaxLength = 128;

    public const int NameMaxLength = 512;

    public required string ClientId { get; init; }

    public required string Name { get; init; }

    public string ApplicationType { get; init; } = OAuthApplicationTypes.Web;

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

        if (!OAuthApplicationTypes.IsSupported(ApplicationType))
        {
            error = "body.applicationType is unsupported";
            return false;
        }

        error = null;
        return true;
    }
}
