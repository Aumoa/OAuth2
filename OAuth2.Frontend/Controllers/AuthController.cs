using System.Net;
using Microsoft.AspNetCore.Mvc;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(IBackendClient backend) : ControllerBase
{
    [HttpGet("redirect")]
    public async Task<IActionResult> RedirectAsync([FromQuery] string code, [FromQuery] string? state, CancellationToken cancellationToken = default)
    {
        var response = await backend.VerifyChallengeAsync(code, state, cancellationToken);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            return Ok();
        }
        else
        {
            return StatusCode((int)response.StatusCode, response.Content);
        }
    }
}
