using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> VerifyAsync([FromServices] IBackendClient backend, [FromQuery] string id, CancellationToken cancellationToken)
    {
        bool exists = await backend.VerifyAccountIdAsync(id, cancellationToken);
        return Ok(exists);
    }
}
