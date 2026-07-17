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
[Route("api/v1/authorization-grants")]
public sealed class AuthorizationGrantsController(
    IAuthorizationCodes authorizationCodes,
    IRememberedSessions rememberedSessions,
    IAccounts accounts,
    IAccountClaims accountClaims,
    IOrganizationMembers organizationMembers,
    IOptions<OAuthOptions> oauthOptions) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
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
            || !OidcScopePolicy.TryNormalize(code.Scope, true, out var grantedScope)
            || !string.Equals(grantedScope, InternalOidcAuthorization.Scope, StringComparison.Ordinal)
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

        var claims = await accountClaims.GetClaimsAsync(code.AccountId, cancellationToken);
        var organizationClaims = await organizationMembers.GetClaimsAsync(
            code.AccountId,
            cancellationToken);
        RememberedSessionGrant? rememberedSession = null;
        if (code.CreateRememberedSession)
        {
            var created = await rememberedSessions.CreateAsync(
                code.AccountId,
                code.AuthTime,
                cancellationToken);
            rememberedSession = new RememberedSessionGrant
            {
                Token = created.Token,
                AuthenticatedAt = DateTimeOffset.FromUnixTimeSeconds(code.AuthTime),
                ExpiresAt = created.ExpiresAt
            };
        }

        return Ok(new GrantedUserInfo
        {
            Scope = grantedScope,
            Claims = OidcUserInfoFactory.Create(
                account,
                claims,
                organizationClaims,
                grantedScope),
            RememberedSession = rememberedSession
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
