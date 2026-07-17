using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/applications")]
public sealed class ApplicationsController(
    ISessionsRepository sessions,
    IBackendClient backend,
    IOptions<BffSessionOptions> sessionOptions)
    : OwnedApplicationsControllerBase(sessions, backend, sessionOptions)
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] CreateApplicationForm form,
        [FromQuery] string? organizationId,
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

        var owner = await ResolveOwnerAsync(organizationId, cancellationToken);
        if (OwnerResolutionError(owner) is { } ownerError)
        {
            return ownerError;
        }

        var response = await Backend.CreateApplicationAsync(owner.OwnerId!, form, cancellationToken);
        return FromBackend(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] string? organizationId,
        CancellationToken cancellationToken)
    {
        var owner = await ResolveOwnerAsync(organizationId, cancellationToken);
        if (OwnerResolutionError(owner) is { } ownerError)
        {
            return ownerError;
        }

        var response = await Backend.GetOwnedApplicationsAsync(owner.OwnerId!, cancellationToken);
        return FromBackend(response);
    }

    [HttpGet("{**id}")]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string id,
        [FromQuery] string? organizationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var owner = await ResolveOwnerAsync(organizationId, cancellationToken);
        if (OwnerResolutionError(owner) is { } ownerError)
        {
            return ownerError;
        }

        var response = await Backend.GetOwnedApplicationAsync(owner.OwnerId!, id, cancellationToken);
        return FromBackend(response);
    }

    [HttpPut("{**id}")]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] string id,
        [FromBody] UpdateApplicationForm form,
        [FromQuery] string? organizationId,
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var owner = await ResolveOwnerAsync(organizationId, cancellationToken);
        if (OwnerResolutionError(owner) is { } ownerError)
        {
            return ownerError;
        }

        var response = await Backend.UpdateApplicationAsync(
            owner.OwnerId!,
            id,
            form,
            cancellationToken);
        return FromBackend(response);
    }

    [HttpDelete("{**id}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string id,
        [FromQuery] string? organizationId,
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var owner = await ResolveOwnerAsync(organizationId, cancellationToken);
        if (OwnerResolutionError(owner) is { } ownerError)
        {
            return ownerError;
        }

        var response = await Backend.DeleteApplicationAsync(owner.OwnerId!, id, cancellationToken);
        return FromBackend(response);
    }

}
