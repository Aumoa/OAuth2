using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using OAuth2.DataTransfer;
using OAuth2.Repositories;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/email-verification-deliveries")]
public sealed class EmailVerificationDeliveriesController(
    IAccounts accounts,
    IEmailVerify emailVerify) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
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
