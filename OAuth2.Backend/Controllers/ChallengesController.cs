using Microsoft.AspNetCore.Mvc;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/challenges")]
public class ChallengesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ChallengeAsync([FromQuery] string code, [FromQuery] string? state, CancellationToken cancellationToken = default)
    {
        return BadRequest("Not implemented yet.");
    }
}
