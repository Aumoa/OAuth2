using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Options;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/authorization-codes")]
public sealed class AuthorizationCodesController(
    IAccounts accounts,
    IAuthorizationCodes authorizationCodes,
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

        var authorization = form.Authorization!;
        var code = await authorizationCodes.PushAsync(
            new AuthorizationCodeBody(
                login.Id,
                authorization.ClientId!,
                normalizedScope,
                authorization.RedirectUri!,
                authorization.Nonce,
                authorization.CodeChallenge,
                authorization.CodeChallengeMethod,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds()),
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
