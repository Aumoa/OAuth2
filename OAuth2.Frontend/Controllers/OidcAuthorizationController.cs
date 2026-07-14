using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OAuth2.OpenId;
using OAuth2.Options;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class OidcAuthorizationController(IOptions<OAuthOptions> oauthOptions) : ControllerBase
{
    [HttpGet("login")]
    public IActionResult StartLogin()
    {
        var codeVerifier = Pkce.CreateCodeVerifier();
        var state = Pkce.CreateCodeVerifier();
        var nonce = Pkce.CreateCodeVerifier();
        var cookieOptions = OidcFlowCookies.Create();

        Response.Cookies.Append(OidcFlowCookies.VerifierName, codeVerifier, cookieOptions);
        Response.Cookies.Append(OidcFlowCookies.StateName, state, cookieOptions);

        var authorizeUri = QueryHelpers.AddQueryString(
            "/api/v1/auth/authorize",
            new Dictionary<string, string?>
            {
                ["client_id"] = oauthOptions.Value.ClientId,
                ["redirect_uri"] = InternalOidcAuthorization.RedirectUri,
                ["response_type"] = "code",
                ["scope"] = InternalOidcAuthorization.Scope,
                ["state"] = state,
                ["nonce"] = nonce,
                ["code_challenge"] = Pkce.CreateCodeChallenge(codeVerifier),
                ["code_challenge_method"] = "S256"
            });

        return Redirect(authorizeUri);
    }

    [HttpGet("authorize")]
    public IActionResult Authorize(
        [FromQuery(Name = "client_id")] string? clientId,
        [FromQuery(Name = "redirect_uri")] string? redirectUri,
        [FromQuery(Name = "response_type")] string? responseType,
        [FromQuery] string? scope,
        [FromQuery] string? state,
        [FromQuery] string? nonce,
        [FromQuery(Name = "code_challenge")] string? codeChallenge,
        [FromQuery(Name = "code_challenge_method")] string? codeChallengeMethod)
    {
        var authorization = new OidcAuthorizationRequest
        {
            ClientId = clientId,
            RedirectUri = redirectUri,
            ResponseType = responseType,
            Scope = scope,
            State = state,
            Nonce = nonce,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod
        };

        if (!InternalOidcAuthorization.TryValidate(
            authorization,
            oauthOptions.Value.ClientId,
            out var normalizedScope,
            out var error))
        {
            return BadRequest(new { error });
        }

        return Redirect(QueryHelpers.AddQueryString(
            "/login",
            new Dictionary<string, string?>
            {
                ["client_id"] = authorization.ClientId,
                ["redirect_uri"] = authorization.RedirectUri,
                ["response_type"] = authorization.ResponseType,
                ["scope"] = normalizedScope,
                ["state"] = authorization.State,
                ["nonce"] = authorization.Nonce,
                ["code_challenge"] = authorization.CodeChallenge,
                ["code_challenge_method"] = authorization.CodeChallengeMethod
            }));
    }
}
