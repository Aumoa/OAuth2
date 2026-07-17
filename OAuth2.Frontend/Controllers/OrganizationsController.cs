using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/organizations")]
public sealed class OrganizationsController(
    ISessionsRepository sessions,
    IBackendClient backend,
    IOptions<BffSessionOptions> sessionOptions) : BackendProxyControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateOrganizationForm form,
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

        var accountId = await GetCurrentAccountIdAsync(
            sessions,
            sessionOptions.Value.CookieName,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Unauthorized();
        }

        var response = await backend.CreateOrganizationAsync(accountId, form, cancellationToken);
        return FromBackend(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var accountId = await GetCurrentAccountIdAsync(
            sessions,
            sessionOptions.Value.CookieName,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Unauthorized();
        }

        var response = await backend.GetOrganizationsAsync(accountId, cancellationToken);
        return FromBackend(response);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var accountId = await GetCurrentAccountIdAsync(
            sessions,
            sessionOptions.Value.CookieName,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Unauthorized();
        }

        var response = await backend.GetOrganizationAsync(accountId, id, cancellationToken);
        return FromBackend(response);
    }
}
