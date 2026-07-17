# OpenID Connect provider

This service acts as an OpenID Provider for applications registered in the
application management screen. Web, Android, iOS, and macOS applications are
public clients and use the Authorization Code flow with mandatory PKCE
(`S256`). Client secrets and refresh tokens are not currently issued.

## Public endpoints

- `/.well-known/openid-configuration`
- `/.well-known/oauth-authorization-server`
- `/authorize`
- `/token`
- `/userinfo`
- `/jwks`

The discovery document is the source of truth for endpoint URLs and supported
capabilities.

## Configuration

Configure the same canonical public issuer in both `OAuth2.Frontend` and
`OAuth2.Backend`:

```json
{
  "OpenId": {
    "Issuer": "https://sso.example.com"
  }
}
```

The issuer must be an HTTPS origin without a path, query, or fragment. HTTP is
accepted only for loopback development origins.

The backend also requires an RSA private key in non-Development environments:

```json
{
  "OpenId": {
    "Issuer": "https://sso.example.com",
    "AccessTokenLifetimeMinutes": 30,
    "SigningKeyPath": "/run/secrets/oidc-signing-key.pem"
  }
}
```

The PEM key must contain an RSA private key of at least 2048 bits. Development
uses an ephemeral key when `SigningKeyPath` is omitted; tokens then become
invalid whenever the backend restarts.

The public reverse proxy must route the endpoints above to `OAuth2.Frontend`.
The frontend forwards private validation, token issuance, user-info, and JWKS
requests to `OAuth2.Backend`.

## Client setup

1. Create an application and use its ID as `client_id`.
2. Register the client's exact callback URL as a redirect URI.
3. Keep `openid` enabled and select any additional claim scopes.
4. Configure the client as a public/no-secret OIDC client with Authorization
   Code flow and PKCE `S256`.
5. Set the authority/issuer to the configured `OpenId:Issuer`.

Redirect URI rules depend on the immutable application type:

- Web: exact HTTPS redirects; exact HTTP loopback redirects are accepted for
  local development.
- Android and iOS: exact claimed HTTPS redirects or reverse-domain private URI
  schemes such as `com.example.app:/oauth/callback`.
- macOS: the Android/iOS options plus dynamic loopback redirects. Register a
  port-free base such as `http://127.0.0.1/oauth/callback`; an authorization
  request may use any ephemeral port while retaining the exact IP, path, and
  query. The token request must repeat that actual redirect URI exactly.

Prefer `127.0.0.1` and `[::1]` over `localhost` for macOS loopback redirects.

Authorization requests must include `client_id`, `redirect_uri`,
`response_type=code`, an `openid` scope, `state`, `code_challenge`, and
`code_challenge_method=S256`. The token request uses form encoding with
`grant_type=authorization_code`, the returned code, the same client and
redirect URI, and the matching `code_verifier`.
