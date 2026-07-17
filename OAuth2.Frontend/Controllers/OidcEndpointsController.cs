using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
public sealed class OidcEndpointsController(
    IBackendClient backend,
    IOptions<OidcProviderOptions> options) : BackendProxyControllerBase
{
    [HttpGet("/.well-known/openid-configuration")]
    [HttpGet("/.well-known/oauth-authorization-server")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public IActionResult GetMetadata()
    {
        OidcEndpointUris.TryNormalizeIssuer(options.Value.Issuer, out var issuer);
        return Ok(new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["issuer"] = issuer!,
            ["authorization_endpoint"] = OidcEndpointUris.Authorization(issuer!),
            ["token_endpoint"] = OidcEndpointUris.Token(issuer!),
            ["userinfo_endpoint"] = OidcEndpointUris.UserInfo(issuer!),
            ["jwks_uri"] = OidcEndpointUris.JsonWebKeys(issuer!),
            ["response_types_supported"] = new[] { "code" },
            ["response_modes_supported"] = new[] { "query" },
            ["grant_types_supported"] = new[] { "authorization_code" },
            ["subject_types_supported"] = new[] { "public" },
            ["id_token_signing_alg_values_supported"] = new[] { "RS256" },
            ["token_endpoint_auth_methods_supported"] = new[] { "none" },
            ["code_challenge_methods_supported"] = new[] { "S256" },
            ["scopes_supported"] = OidcScopePolicy.ClaimScopes,
            ["claims_supported"] = OidcClaimPolicy.ClaimNames
        });
    }

    [HttpPost("/token")]
    [Consumes("application/x-www-form-urlencoded")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ExchangeAuthorizationCodeAsync(
        [FromForm(Name = "grant_type")] string? grantType,
        [FromForm(Name = "code")] string? code,
        [FromForm(Name = "client_id")] string? clientId,
        [FromForm(Name = "redirect_uri")] string? redirectUri,
        [FromForm(Name = "code_verifier")] string? codeVerifier,
        CancellationToken cancellationToken)
    {
        Response.Headers.Pragma = "no-cache";
        if (Request.Headers.ContainsKey("Authorization"))
        {
            return BadRequest(new { error = "invalid_client" });
        }

        if (!string.Equals(grantType, "authorization_code", StringComparison.Ordinal))
        {
            return BadRequest(new { error = "unsupported_grant_type" });
        }

        if (string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(redirectUri)
            || string.IsNullOrWhiteSpace(codeVerifier))
        {
            return BadRequest(new { error = "invalid_request" });
        }

        var response = await backend.ExchangeOidcAuthorizationCodeAsync(
            new OidcTokenExchange
            {
                Code = code,
                ClientId = clientId,
                RedirectUri = redirectUri,
                CodeVerifier = codeVerifier
            },
            cancellationToken);
        return FromBackend(new BackendResponse(
            response.StatusCode,
            response.Content,
            response.ContentType));
    }

    [HttpGet("/userinfo")]
    [HttpPost("/userinfo")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> GetUserInfoAsync(CancellationToken cancellationToken)
    {
        var authorization = Request.Headers.Authorization.ToString();
        const string BearerPrefix = "Bearer ";
        if (!authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(authorization[BearerPrefix.Length..])
            || authorization[BearerPrefix.Length..].Contains(' '))
        {
            Response.Headers.WWWAuthenticate = "Bearer error=\"invalid_token\"";
            return Unauthorized(new { error = "invalid_token" });
        }

        var response = await backend.GetOidcUserInfoAsync(
            authorization[BearerPrefix.Length..],
            cancellationToken);
        if ((int)response.StatusCode == StatusCodes.Status401Unauthorized)
        {
            Response.Headers.WWWAuthenticate = "Bearer error=\"invalid_token\"";
        }

        return FromBackend(new BackendResponse(
            response.StatusCode,
            response.Content,
            response.ContentType));
    }

    [HttpGet("/jwks")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> GetJsonWebKeysAsync(CancellationToken cancellationToken)
    {
        var response = await backend.GetOidcJsonWebKeysAsync(cancellationToken);
        return FromBackend(new BackendResponse(
            response.StatusCode,
            response.Content,
            response.ContentType));
    }
}
