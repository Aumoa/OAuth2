using System.Diagnostics.CodeAnalysis;
using OAuth2.OpenId;

namespace OAuth2.DataTransfer;

public sealed record RememberedLoginForm
{
    public required string Token { get; init; }

    public OidcAuthorizationRequest? Authorization { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(Token))
        {
            error = "body.token is missing";
            return false;
        }

        if (Authorization is null)
        {
            error = "body.authorization is missing";
            return false;
        }

        error = null;
        return true;
    }
}
