using Microsoft.AspNetCore.Mvc;
using OAuth2.DataTransfer;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/email-verifications")]
public sealed class EmailVerificationsController(IAccounts accounts) : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> VerifyAsync(
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
}
