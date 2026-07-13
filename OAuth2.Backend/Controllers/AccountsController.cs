using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Options;
using OAuth2.Repositories;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/accounts")]
public class AccountsController(
    IAccounts accounts,
    IAuthorizationCodes authorizationCodes,
    IEmailVerify emailVerify,
    IOptions<OAuthOptions> oauthOptions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] string id,
        CancellationToken cancellationToken = default)
    {
        var account = await accounts.GetAccountAsync(id, cancellationToken);
        return account is null ? NotFound() : NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> PostAsync(
        [FromBody] RegisterForm form,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var registration = await accounts.AddAccountAsync(
            form.Id,
            form.Password,
            form.FullName,
            form.Email,
            cancellationToken);
        if (registration is null)
        {
            return Conflict();
        }

        await emailVerify.SendAsync(
            registration.Sub,
            registration.VerifyCode,
            new MailAddress(form.Email),
            cancellationToken);

        return Ok(new EmailVerificationChallenge
        {
            Sub = registration.Sub
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(
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

    [HttpPost("email/verify")]
    public async Task<IActionResult> VerifyEmailAsync(
        [FromBody] EmailVerificationForm form,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var verified = await accounts.VerifyEmailAsync(form.Sub, form.Code, cancellationToken);
        return verified ? NoContent() : BadRequest("email verification failed");
    }

    [HttpPost("email/resend")]
    public async Task<IActionResult> ResendEmailVerificationAsync(
        [FromBody] EmailVerificationResendForm form,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var delivery = await accounts.RefreshEmailVerificationAsync(form.Sub, cancellationToken);
        if (delivery is not null)
        {
            await emailVerify.SendAsync(
                delivery.Sub,
                delivery.VerifyCode,
                new MailAddress(delivery.Email),
                cancellationToken);
        }

        // Do not reveal whether the supplied subject exists or is already verified.
        return NoContent();
    }
}
