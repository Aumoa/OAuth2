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

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string id,
        [FromBody] DeleteOrganizationForm form,
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

        var accountId = await GetCurrentAccountIdAsync(
            sessions,
            sessionOptions.Value.CookieName,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Unauthorized();
        }

        var response = await backend.DeleteOrganizationAsync(
            accountId,
            id,
            form,
            cancellationToken);
        return FromBackend(response);
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembersAsync(
        [FromRoute] string id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = OrganizationMemberPage.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)
            || page <= 0
            || pageSize <= 0
            || pageSize > OrganizationMemberPage.MaxPageSize)
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

        var response = await backend.GetOrganizationMembersAsync(
            accountId,
            id,
            page,
            pageSize,
            cancellationToken);
        return FromBackend(response);
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMemberAsync(
        [FromRoute] string id,
        [FromBody] AddOrganizationMemberForm form,
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

        var accountId = await GetCurrentAccountIdAsync(
            sessions,
            sessionOptions.Value.CookieName,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Unauthorized();
        }

        var response = await backend.AddOrganizationMemberAsync(
            accountId,
            id,
            form,
            cancellationToken);
        return FromBackend(response);
    }

    [HttpPut("{id}/members/{memberAccountId}")]
    public async Task<IActionResult> UpdateMemberAsync(
        [FromRoute] string id,
        [FromRoute] string memberAccountId,
        [FromBody] UpdateOrganizationMemberForm form,
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(memberAccountId))
        {
            return BadRequest();
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

        var response = await backend.UpdateOrganizationMemberAsync(
            accountId,
            id,
            memberAccountId,
            form,
            cancellationToken);
        return FromBackend(response);
    }

    [HttpDelete("{id}/members/{memberAccountId}")]
    public async Task<IActionResult> DeleteMemberAsync(
        [FromRoute] string id,
        [FromRoute] string memberAccountId,
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(memberAccountId))
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

        var response = await backend.DeleteOrganizationMemberAsync(
            accountId,
            id,
            memberAccountId,
            cancellationToken);
        return FromBackend(response);
    }

    [HttpPut("{id}/owner")]
    public async Task<IActionResult> TransferOwnershipAsync(
        [FromRoute] string id,
        [FromBody] TransferOrganizationOwnershipForm form,
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

        var accountId = await GetCurrentAccountIdAsync(
            sessions,
            sessionOptions.Value.CookieName,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return Unauthorized();
        }

        var response = await backend.TransferOrganizationOwnershipAsync(
            accountId,
            id,
            form,
            cancellationToken);
        return FromBackend(response);
    }
}
