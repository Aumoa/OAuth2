using Microsoft.AspNetCore.Mvc;
using OAuth2.DataTransfer;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/email-verification-deliveries")]
public sealed class EmailVerificationDeliveriesController(IBackendClient backend) : BackendProxyControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] EmailVerificationResendForm form,
        CancellationToken cancellationToken)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var response = await backend.CreateEmailVerificationDeliveryAsync(
            form,
            Request.Headers.AcceptLanguage.ToString(),
            cancellationToken);
        return FromBackend(response);
    }
}
