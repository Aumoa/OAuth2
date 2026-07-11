using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using OAuth2.DataTransfer;
using OAuth2.Repositories;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/accounts")]
public class AccountsController(IAccounts accounts, IEmailVerify emailVerify) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] string id,
        CancellationToken cancellationToken = default)
    {
        var account = await accounts.GetAccountAsync(id, cancellationToken);
        if (account == null)
        {
            return NotFound();
        }

        return NoContent();
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

        return NoContent();
    }
}
