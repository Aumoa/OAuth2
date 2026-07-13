using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Options;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/challenges")]
public class ChallengesController(
    IAuthorizationCodes authorizationCodes,
    IAccounts accounts,
    IOptions<OAuthOptions> oauthOptions) : ControllerBase
{
    [HttpPost("exchange")]
    public async Task<IActionResult> ExchangeAsync(
        [FromBody] AuthorizationCodeExchange exchange,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(exchange.Code)
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
        if (!string.Equals(code.ClientId, oauthOptions.Value.ClientId, StringComparison.Ordinal)
            || !string.Equals(exchange.ClientId, code.ClientId, StringComparison.Ordinal)
            || !string.Equals(code.RedirectUri, InternalOidcAuthorization.RedirectUri, StringComparison.Ordinal)
            || !string.Equals(exchange.RedirectUri, code.RedirectUri, StringComparison.Ordinal)
            || !Pkce.Validate(exchange.CodeVerifier, code.CodeChallenge, code.CodeChallengeMethod))
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

        return Ok(new SessionUser
        {
            Id = account.Id,
            Sub = account.Sub,
            Email = account.Email,
            Name = account.Name
        });
    }

    private BadRequestObjectResult InvalidGrant()
    {
        return BadRequest(new
        {
            error = "invalid_grant"
        });
    }
}
