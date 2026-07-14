using Microsoft.AspNetCore.Mvc;
using OAuth2.DataTransfer;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/email-verifications")]
public sealed class EmailVerificationsController(IBackendClient backend) : BackendProxyControllerBase
{
    [HttpPut]
    public async Task<IActionResult> VerifyAsync(
        [FromBody] EmailVerificationForm form,
        CancellationToken cancellationToken)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        return FromBackend(await backend.VerifyEmailAsync(form, cancellationToken));
    }
}
