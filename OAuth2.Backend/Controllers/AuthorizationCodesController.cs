using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Repositories;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/authorization-codes")]
public sealed class AuthorizationCodesController(
    IAccounts accounts,
    IAccountClaims accountClaims,
    IAuthorizationCodes authorizationCodes,
    IRememberedSessions rememberedSessions,
    OidcAuthorizationRequestValidator authorizationValidator) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] LoginForm form,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var validation = await authorizationValidator.ValidateAsync(
            form.Authorization,
            cancellationToken);
        if (!validation.IsValid || validation.NormalizedScope is null)
        {
            return BadRequest(validation.Error);
        }

        var login = await accounts.LoginAsync(form.Id, form.Password, cancellationToken);
        if (login is null)
        {
            return Unauthorized();
        }

        if (!login.EmailVerified)
        {
            return Ok(new LoginResponse
            {
                State = LoginStates.EmailVerificationRequired,
                Sub = login.Sub
            });
        }

        return await CreateAuthenticatedResponseAsync(
            login.Id,
            form.Authorization!,
            validation.NormalizedScope,
            DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            true,
            cancellationToken);
    }

    [HttpPost("remembered")]
    public async Task<IActionResult> CreateFromRememberedSessionAsync(
        [FromBody] RememberedLoginForm form,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var validation = await authorizationValidator.ValidateAsync(
            form.Authorization,
            cancellationToken);
        if (!validation.IsValid || validation.NormalizedScope is null)
        {
            return BadRequest(validation.Error);
        }

        var rememberedSession = await rememberedSessions.GetAsync(
            form.Token,
            cancellationToken);
        if (rememberedSession is null)
        {
            return Unauthorized();
        }

        var account = await accounts.GetAccountAsync(
            rememberedSession.AccountId,
            cancellationToken);
        if (account is null
            || !string.IsNullOrEmpty(account.VerifyCode)
            || string.IsNullOrWhiteSpace(account.Id))
        {
            await rememberedSessions.DeleteAsync(form.Token, cancellationToken);
            return Unauthorized();
        }

        return await CreateAuthenticatedResponseAsync(
            account.Id,
            form.Authorization!,
            validation.NormalizedScope,
            rememberedSession.AuthTime,
            false,
            cancellationToken);
    }

    private async Task<IActionResult> CreateAuthenticatedResponseAsync(
        string accountId,
        OidcAuthorizationRequest authorization,
        string normalizedScope,
        long authTime,
        bool createBrowserSession,
        CancellationToken cancellationToken)
    {
        var code = await authorizationCodes.PushAsync(
            new AuthorizationCodeBody(
                accountId,
                authorization.ClientId!,
                normalizedScope,
                authorization.RedirectUri!,
                authorization.Nonce,
                authorization.CodeChallenge,
                authorization.CodeChallengeMethod,
                authTime,
                false),
            cancellationToken);
        var redirectUri = QueryHelpers.AddQueryString(
            authorization.RedirectUri!,
            new Dictionary<string, string?>
            {
                ["code"] = code,
                ["state"] = authorization.State
            });

        GrantedUserInfo? sessionGrant = null;
        if (createBrowserSession)
        {
            var account = await accounts.GetAccountAsync(accountId, cancellationToken);
            if (account is null)
            {
                return Unauthorized();
            }

            var claims = await accountClaims.GetClaimsAsync(accountId, cancellationToken);
            var rememberedSession = await rememberedSessions.CreateAsync(
                accountId,
                authTime,
                cancellationToken);
            sessionGrant = new GrantedUserInfo
            {
                Scope = InternalOidcAuthorization.Scope,
                Claims = OidcUserInfoFactory.Create(
                    account,
                    claims,
                    InternalOidcAuthorization.Scope),
                RememberedSession = new RememberedSessionGrant
                {
                    Token = rememberedSession.Token,
                    AuthenticatedAt = DateTimeOffset.FromUnixTimeSeconds(authTime),
                    ExpiresAt = rememberedSession.ExpiresAt
                }
            };
        }

        return Ok(new LoginResponse
        {
            State = LoginStates.Authenticated,
            RedirectUri = redirectUri,
            SessionGrant = sessionGrant
        });
    }
}
