using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Options;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/authorization-codes")]
public sealed class AuthorizationCodesController(
    IAccounts accounts,
    IAuthorizationCodes authorizationCodes,
    IRememberedSessions rememberedSessions,
    IOptions<OAuthOptions> oauthOptions) : ControllerBase
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

        if (!InternalOidcAuthorization.TryValidate(
            form.Authorization,
            oauthOptions.Value.ClientId,
            out var normalizedScope,
            out error))
        {
            return BadRequest(error);
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
            normalizedScope,
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

        if (!InternalOidcAuthorization.TryValidate(
            form.Authorization,
            oauthOptions.Value.ClientId,
            out var normalizedScope,
            out error))
        {
            return BadRequest(error);
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
            normalizedScope,
            rememberedSession.AuthTime,
            false,
            cancellationToken);
    }

    private async Task<IActionResult> CreateAuthenticatedResponseAsync(
        string accountId,
        OidcAuthorizationRequest authorization,
        string normalizedScope,
        long authTime,
        bool createRememberedSession,
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
                createRememberedSession),
            cancellationToken);
        var redirectUri = QueryHelpers.AddQueryString(
            authorization.RedirectUri!,
            new Dictionary<string, string?>
            {
                ["code"] = code,
                ["state"] = authorization.State
            });

        return Ok(new LoginResponse
        {
            State = LoginStates.Authenticated,
            RedirectUri = redirectUri
        });
    }
}
