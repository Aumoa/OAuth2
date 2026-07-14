using Microsoft.AspNetCore.Mvc;
using OAuth2.DataTransfer;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/remembered-sessions")]
public sealed class RememberedSessionsController(
    IRememberedSessions rememberedSessions) : ControllerBase
{
    [HttpDelete]
    public async Task<IActionResult> DeleteAsync(
        [FromBody] RememberedSessionRevocation revocation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(revocation.Token))
        {
            return BadRequest();
        }

        await rememberedSessions.DeleteAsync(revocation.Token, cancellationToken);
        return NoContent();
    }
}
