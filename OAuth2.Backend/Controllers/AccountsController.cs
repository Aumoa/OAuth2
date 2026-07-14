using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using OAuth2.DataTransfer;
using OAuth2.Repositories;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/accounts")]
public sealed class AccountsController(
    IAccounts accounts,
    IEmailVerify emailVerify) : ControllerBase
{
    [HttpHead("{id}")]
    public async Task<IActionResult> ExistsAsync(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var account = await accounts.GetAccountAsync(id, cancellationToken);
        return account is null ? NotFound() : NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
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

        return CreatedAtAction(
            nameof(ExistsAsync),
            new { id = form.Id },
            new EmailVerificationChallenge
            {
                Sub = registration.Sub
            });
    }
}
