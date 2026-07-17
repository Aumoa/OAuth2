using Microsoft.AspNetCore.Mvc;
using OAuth2.DataTransfer;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/authorization-codes")]
public sealed class AuthorizationCodesController(
    IBackendClient backend,
    BrowserSessionSignIn browserSignIn) : BackendProxyControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] LoginForm form,
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var response = await backend.CreateAuthorizationCodeAsync(form, cancellationToken);
        if (response.Value is null
            || !string.Equals(
                response.Value.State,
                LoginStates.Authenticated,
                StringComparison.Ordinal))
        {
            return FromBackend(new BackendResponse(
                response.StatusCode,
                response.Content,
                response.ContentType));
        }

        var login = response.Value;
        if (login.SessionGrant is not null)
        {
            await browserSignIn.SignInAsync(
                HttpContext,
                login.SessionGrant,
                cancellationToken);
        }

        return Ok(login with { SessionGrant = null });
    }
}
