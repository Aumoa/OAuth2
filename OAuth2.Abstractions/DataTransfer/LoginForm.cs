using System.Diagnostics.CodeAnalysis;
using OAuth2.OpenId;

namespace OAuth2.DataTransfer;

public sealed record LoginForm
{
    public required string Id { get; init; }

    public required string Password { get; init; }

    public OidcAuthorizationRequest? Authorization { get; init; }

    public bool Verify([NotNullWhen(false)] out string? error)
    {
        if (string.IsNullOrWhiteSpace(Id))
        {
            error = "body.id is missing";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            error = "body.password is missing";
            return false;
        }

        error = null;
        return true;
    }
}
