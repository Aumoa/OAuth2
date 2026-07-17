using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OAuth2.OpenId;
using OAuth2.Options;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class OidcAuthorizationController(
    IOptions<OAuthOptions> oauthOptions,
    IBackendClient backend) : ControllerBase
{
    [HttpPost("authorization-requests/validation")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> ValidateAuthorizationRequestAsync(
        [FromBody] OidcAuthorizationRequest authorization,
        CancellationToken cancellationToken)
    {
        var response = await backend.ValidateOidcAuthorizationAsync(
            authorization,
            cancellationToken);
        return response.Value is not null
            ? Ok(response.Value)
            : StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "temporarily_unavailable"
            });
    }

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
    [HttpGet("/authorize")]
    public async Task<IActionResult> AuthorizeAsync(
        [FromQuery(Name = "client_id")] string? clientId,
        [FromQuery(Name = "redirect_uri")] string? redirectUri,
        [FromQuery(Name = "response_type")] string? responseType,
        [FromQuery] string? scope,
        [FromQuery] string? state,
        [FromQuery] string? nonce,
        [FromQuery] string? prompt,
        [FromQuery(Name = "code_challenge")] string? codeChallenge,
        [FromQuery(Name = "code_challenge_method")] string? codeChallengeMethod,
        CancellationToken cancellationToken)
    {
        var authorization = new OidcAuthorizationRequest
        {
            ClientId = clientId,
            RedirectUri = redirectUri,
            ResponseType = responseType,
            Scope = scope,
            State = state,
            Nonce = nonce,
            Prompt = prompt,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod
        };

        var backendResponse = await backend.ValidateOidcAuthorizationAsync(
            authorization,
            cancellationToken);
        if (backendResponse.Value is null)
        {
            return StatusCode(StatusCodes.Status502BadGateway, new
            {
                error = "temporarily_unavailable"
            });
        }

        var validation = backendResponse.Value;
        if (!validation.IsValid || validation.NormalizedScope is null)
        {
            if (validation.CanRedirect && !string.IsNullOrWhiteSpace(redirectUri))
            {
                return Redirect(QueryHelpers.AddQueryString(
                    redirectUri,
                    new Dictionary<string, string?>
                    {
                        ["error"] = validation.Error ?? "invalid_request",
                        ["state"] = state
                    }));
            }

            return BadRequest(new
            {
                error = validation.Error ?? "invalid_request"
            });
        }

        return Redirect(QueryHelpers.AddQueryString(
            "/login",
            new Dictionary<string, string?>
            {
                ["client_id"] = authorization.ClientId,
                ["redirect_uri"] = authorization.RedirectUri,
                ["response_type"] = authorization.ResponseType,
                ["scope"] = validation.NormalizedScope,
                ["state"] = authorization.State,
                ["nonce"] = authorization.Nonce,
                ["prompt"] = authorization.Prompt,
                ["code_challenge"] = authorization.CodeChallenge,
                ["code_challenge_method"] = authorization.CodeChallengeMethod
            }));
    }
}
