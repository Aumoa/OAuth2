using OAuth2.OpenId;

namespace OAuth2.Options;

public sealed record OAuthOptions
{
    public required string BackendUrl { get; init; }

    public string ClientId { get; init; } = InternalOidcAuthorization.DefaultClientId;
}
