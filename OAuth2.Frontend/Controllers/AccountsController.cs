using System.Net;
using Microsoft.AspNetCore.Mvc;
using OAuth2.DataTransfer;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/accounts")]
public sealed class AccountsController(IBackendClient backend) : BackendProxyControllerBase
{
    [HttpHead("{id}")]
    public async Task<IActionResult> ExistsAsync(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        return await backend.AccountExistsAsync(id, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] RegisterForm form,
        CancellationToken cancellationToken)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var response = await backend.RegisterAccountAsync(
            form,
            Request.Headers.AcceptLanguage.ToString(),
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Created)
        {
            Response.Headers.Location = Url.Action(nameof(ExistsAsync), new { id = form.Id })
                ?? $"/api/v1/accounts/{Uri.EscapeDataString(form.Id)}";
        }

        return FromBackend(response);
    }
}
