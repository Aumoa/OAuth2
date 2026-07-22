using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/applications/{clientId}/roles")]
public sealed class ApplicationRolesController(
    ISessionsRepository sessions,
    IBackendClient backend,
    IOptions<BffSessionOptions> sessionOptions)
    : OwnedApplicationsControllerBase(sessions, backend, sessionOptions)
{
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

        return FromBackend(await Backend.GetApplicationRolesAsync(
            owner.OwnerId!,
            clientId,
            cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromRoute] string clientId,
        [FromQuery] string? organizationId,
        [FromBody] CreateApplicationRoleForm form,
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

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var owner = await ResolveOwnerAsync(organizationId, cancellationToken);
        if (OwnerResolutionError(owner) is { } ownerError)
        {
            return ownerError;
        }

        return FromBackend(await Backend.CreateApplicationRoleAsync(
            owner.OwnerId!,
            clientId,
            form,
            cancellationToken));
    }

    [HttpDelete("{roleId}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string clientId,
        [FromRoute] string roleId,
        [FromQuery] string? organizationId,
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(roleId))
        {
            return BadRequest();
        }

        var owner = await ResolveOwnerAsync(organizationId, cancellationToken);
        if (OwnerResolutionError(owner) is { } ownerError)
        {
            return ownerError;
        }

        return FromBackend(await Backend.DeleteApplicationRoleAsync(
            owner.OwnerId!,
            clientId,
            roleId,
            cancellationToken));
    }

    [HttpGet("{roleId}/members")]
    public async Task<IActionResult> GetMembersAsync(
        [FromRoute] string clientId,
        [FromRoute] string roleId,
        [FromQuery] string? organizationId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ApplicationRoleMemberPage.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(roleId)
            || page <= 0
            || pageSize <= 0
            || pageSize > ApplicationRoleMemberPage.MaxPageSize)
        {
            return BadRequest();
        }

        var owner = await ResolveOwnerAsync(organizationId, cancellationToken);
        if (OwnerResolutionError(owner) is { } ownerError)
        {
            return ownerError;
        }

        return FromBackend(await Backend.GetApplicationRoleMembersAsync(
            owner.OwnerId!,
            clientId,
            roleId,
            page,
            pageSize,
            cancellationToken));
    }

    [HttpPost("{roleId}/members")]
    public async Task<IActionResult> AddMemberAsync(
        [FromRoute] string clientId,
        [FromRoute] string roleId,
        [FromQuery] string? organizationId,
        [FromBody] AddApplicationRoleMemberForm form,
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(roleId))
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

        return FromBackend(await Backend.AddApplicationRoleMemberAsync(
            owner.OwnerId!,
            clientId,
            roleId,
            form,
            cancellationToken));
    }

    [HttpDelete("{roleId}/members/{accountId}")]
    public async Task<IActionResult> DeleteMemberAsync(
        [FromRoute] string clientId,
        [FromRoute] string roleId,
        [FromRoute] string accountId,
        [FromQuery] string? organizationId,
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(roleId)
            || string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest();
        }

        var owner = await ResolveOwnerAsync(organizationId, cancellationToken);
        if (OwnerResolutionError(owner) is { } ownerError)
        {
            return ownerError;
        }

        return FromBackend(await Backend.DeleteApplicationRoleMemberAsync(
            owner.OwnerId!,
            clientId,
            roleId,
            accountId,
            cancellationToken));
    }
}
