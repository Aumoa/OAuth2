using System.Diagnostics.CodeAnalysis;
using OAuth2.OpenId;

namespace OAuth2.DataTransfer;

public sealed record RememberedAuthorizationForm
{
    public required OidcAuthorizationRequest Authorization { get; init; }

    public bool ConsentGranted { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (Authorization is null)
        {
            error = "body.authorization is missing";
            return false;
        }

        error = null;
        return true;
    }
}
