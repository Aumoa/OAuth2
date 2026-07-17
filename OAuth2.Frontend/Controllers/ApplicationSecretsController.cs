using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/applications/{clientId}/secrets")]
public sealed class ApplicationSecretsController(
    ISessionsRepository sessions,
    IBackendClient backend,
    IOptions<BffSessionOptions> sessionOptions)
    : OwnedApplicationsControllerBase(sessions, backend, sessionOptions)
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromRoute] string clientId,
        [FromQuery] string? organizationId,
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(clientId))
        {
            return BadRequest();
        }

        var owner = await ResolveOwnerAsync(organizationId, cancellationToken);
        if (OwnerResolutionError(owner) is { } ownerError)
        {
            return ownerError;
        }

        var response = await Backend.CreateApplicationSecretAsync(
            owner.OwnerId!,
            clientId,
            cancellationToken);
        return FromBackend(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string clientId,
        [FromQuery] string? organizationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            return BadRequest();
        }

        var owner = await ResolveOwnerAsync(organizationId, cancellationToken);
        if (OwnerResolutionError(owner) is { } ownerError)
        {
            return ownerError;
        }

        var response = await Backend.GetApplicationSecretsAsync(
            owner.OwnerId!,
            clientId,
            cancellationToken);
        return FromBackend(response);
    }

    [HttpDelete("{secretId:long}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string clientId,
        [FromRoute] long secretId,
        [FromQuery] string? organizationId,
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(clientId) || secretId <= 0)
        {
            return BadRequest();
        }

        var owner = await ResolveOwnerAsync(organizationId, cancellationToken);
        if (OwnerResolutionError(owner) is { } ownerError)
        {
            return ownerError;
        }

        var response = await Backend.DeleteApplicationSecretAsync(
            owner.OwnerId!,
            clientId,
            secretId,
            cancellationToken);
        return FromBackend(response);
    }
}
