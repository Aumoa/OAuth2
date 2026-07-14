using Microsoft.AspNetCore.Mvc;
using OAuth2.DataTransfer;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/authorization-codes")]
public sealed class AuthorizationCodesController(IBackendClient backend) : BackendProxyControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] LoginForm form,
        CancellationToken cancellationToken)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        return FromBackend(await backend.CreateAuthorizationCodeAsync(form, cancellationToken));
    }
}
