using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Options;
using OAuth2.Repositories;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/oidc")]
public sealed class OidcController(
    OidcAuthorizationRequestValidator authorizationValidator,
    IAuthorizationCodes authorizationCodes,
    IApplications applications,
    IAccounts accounts,
    IAccountClaims accountClaims,
    OidcTokenIssuer tokenIssuer,
    OidcSigningKey signingKey,
    IOptions<OAuthOptions> oauthOptions) : ControllerBase
{
    [HttpPost("authorization-requests/validation")]
    public async Task<IActionResult> ValidateAuthorizationRequestAsync(
        [FromBody] OidcAuthorizationRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await authorizationValidator.ValidateAsync(request, cancellationToken));
    }

    [HttpPost("token")]
    public async Task<IActionResult> ExchangeAuthorizationCodeAsync(
        [FromBody] OidcTokenExchange exchange,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(exchange.Code)
            || string.IsNullOrWhiteSpace(exchange.ClientId)
            || string.IsNullOrWhiteSpace(exchange.RedirectUri)
            || string.IsNullOrWhiteSpace(exchange.CodeVerifier))
        {
            return InvalidGrant();
        }

        var authorizationCode = await authorizationCodes.PopAsync(
            exchange.Code,
            cancellationToken);
        if (!authorizationCode.HasValue)
        {
            return InvalidGrant();
        }

        var code = authorizationCode.Value;
        if (string.Equals(code.ClientId, oauthOptions.Value.ClientId, StringComparison.Ordinal)
            || !string.Equals(exchange.ClientId, code.ClientId, StringComparison.Ordinal)
            || !string.Equals(exchange.RedirectUri, code.RedirectUri, StringComparison.Ordinal)
            || !Pkce.Validate(
                exchange.CodeVerifier,
                code.CodeChallenge,
                code.CodeChallengeMethod))
        {
            return InvalidGrant();
        }

        var application = await applications.GetApplicationAsync(
            code.ClientId,
            cancellationToken);
        if (application is null
            || !OidcAuthorizationPolicy.TryValidateRegisteredApplication(
                new OidcAuthorizationRequest
                {
                    ClientId = code.ClientId,
                    RedirectUri = code.RedirectUri,
                    ResponseType = "code",
                    Scope = code.Scope,
                    State = "token-exchange",
                    CodeChallenge = code.CodeChallenge,
                    CodeChallengeMethod = code.CodeChallengeMethod
                },
                application,
                out var normalizedScope,
                out _,
                out _)
            || !string.Equals(normalizedScope, code.Scope, StringComparison.Ordinal))
        {
            return InvalidGrant();
        }

        var account = await accounts.GetAccountAsync(code.AccountId, cancellationToken);
        if (account is null
            || !string.IsNullOrEmpty(account.VerifyCode)
            || string.IsNullOrWhiteSpace(account.Id)
            || string.IsNullOrWhiteSpace(account.Sub)
            || string.IsNullOrWhiteSpace(account.Email)
            || string.IsNullOrWhiteSpace(account.Name))
        {
            return InvalidGrant();
        }

        var claims = await accountClaims.GetClaimsAsync(code.AccountId, cancellationToken);
        return Ok(tokenIssuer.Issue(account, claims, code));
    }

    [HttpPost("userinfo")]
    public IActionResult GetUserInfo([FromBody] OidcAccessTokenRequest request)
    {
        return tokenIssuer.TryGetUserInfo(request.Token, out var userInfo)
            ? Ok(userInfo)
            : Unauthorized(new { error = "invalid_token" });
    }

    [HttpGet("jwks")]
    public IActionResult GetJsonWebKeys()
    {
        return Ok(signingKey.GetJsonWebKeySet());
    }

    private BadRequestObjectResult InvalidGrant()
    {
        return BadRequest(new { error = "invalid_grant" });
    }
}
