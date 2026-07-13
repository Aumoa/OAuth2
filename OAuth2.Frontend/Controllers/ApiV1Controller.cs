using Microsoft.AspNetCore.Mvc;
using OAuth2.DataTransfer;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1")]
public class ApiV1Controller : ControllerBase
{
    [HttpGet("session")]
    public async Task<IActionResult> GetSessionAsync(CancellationToken cancellationToken)
    {
        await Task.Yield();
        cancellationToken.ThrowIfCancellationRequested();
        return Unauthorized();
    }

    [HttpGet("accounts/verify")]
    public async Task<IActionResult> VerifyAsync(
        [FromServices] IBackendClient backend,
        [FromQuery] string id,
        CancellationToken cancellationToken)
    {
        bool exists = await backend.VerifyAccountIdAsync(id, cancellationToken);
        return Ok(exists);
    }

    [HttpPost("accounts")]
    public async Task<IActionResult> RegisterAsync(
        [FromServices] IBackendClient backend,
        [FromBody] RegisterForm form,
        CancellationToken cancellationToken)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var response = await backend.RegisterAccountAsync(
            form,
            Request.Headers.AcceptLanguage.ToString(),
            cancellationToken);
        return FromBackend(response);
    }

    [HttpPost("accounts/login")]
    public async Task<IActionResult> LoginAsync(
        [FromServices] IBackendClient backend,
        [FromBody] LoginForm form,
        CancellationToken cancellationToken)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        return FromBackend(await backend.LoginAsync(form, cancellationToken));
    }

    [HttpPost("accounts/email/verify")]
    public async Task<IActionResult> VerifyEmailAsync(
        [FromServices] IBackendClient backend,
        [FromBody] EmailVerificationForm form,
        CancellationToken cancellationToken)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        return FromBackend(await backend.VerifyEmailAsync(form, cancellationToken));
    }

    [HttpPost("accounts/email/resend")]
    public async Task<IActionResult> ResendEmailVerificationAsync(
        [FromServices] IBackendClient backend,
        [FromBody] EmailVerificationResendForm form,
        CancellationToken cancellationToken)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var response = await backend.ResendEmailVerificationAsync(
            form,
            Request.Headers.AcceptLanguage.ToString(),
            cancellationToken);
        return FromBackend(response);
    }

    private IActionResult FromBackend(BackendResponse response)
    {
        if (string.IsNullOrEmpty(response.Content))
        {
            return StatusCode((int)response.StatusCode);
        }

        return new ContentResult
        {
            Content = response.Content,
            ContentType = response.ContentType ?? "application/json; charset=utf-8",
            StatusCode = (int)response.StatusCode
        };
    }
}
