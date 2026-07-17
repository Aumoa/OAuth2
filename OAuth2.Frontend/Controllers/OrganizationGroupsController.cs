using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/organizations/{organizationId}/groups")]
public sealed class OrganizationGroupsController(
    ISessionsRepository sessions,
    IBackendClient backend,
    IOptions<BffSessionOptions> sessionOptions) : BackendProxyControllerBase
{
    [HttpGet]
    public Task<IActionResult> GetAsync(
        [FromRoute] string organizationId,
        CancellationToken cancellationToken) =>
        ProxyAsync(
            organizationId,
            (accountId, token) => backend.GetOrganizationGroupsAsync(
                accountId,
                organizationId,
                token),
            cancellationToken);

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromRoute] string organizationId,
        [FromBody] CreateOrganizationGroupForm form,
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

        return await ProxyAsync(
            organizationId,
            (accountId, token) => backend.CreateOrganizationGroupAsync(
                accountId,
                organizationId,
                form,
                token),
            cancellationToken);
    }

    [HttpGet("{groupId}")]
    public Task<IActionResult> GetAsync(
        [FromRoute] string organizationId,
        [FromRoute] string groupId,
        CancellationToken cancellationToken) =>
        ProxyAsync(
            organizationId,
            groupId,
            (accountId, token) => backend.GetOrganizationGroupAsync(
                accountId,
                organizationId,
                groupId,
                token),
            cancellationToken);

    [HttpGet("{groupId}/members")]
    public Task<IActionResult> GetMembersAsync(
        [FromRoute] string organizationId,
        [FromRoute] string groupId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = OrganizationMemberPage.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (page <= 0 || pageSize <= 0 || pageSize > OrganizationMemberPage.MaxPageSize)
        {
            return Task.FromResult<IActionResult>(BadRequest());
        }

        return ProxyAsync(
            organizationId,
            groupId,
            (accountId, token) => backend.GetOrganizationGroupMembersAsync(
                accountId,
                organizationId,
                groupId,
                page,
                pageSize,
                token),
            cancellationToken);
    }

    [HttpPost("{groupId}/members")]
    public async Task<IActionResult> AddMemberAsync(
        [FromRoute] string organizationId,
        [FromRoute] string groupId,
        [FromBody] AddOrganizationGroupMemberForm form,
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

        return await ProxyAsync(
            organizationId,
            groupId,
            (accountId, token) => backend.AddOrganizationGroupMemberAsync(
                accountId,
                organizationId,
                groupId,
                form,
                token),
            cancellationToken);
    }

    [HttpDelete("{groupId}/members/{memberAccountId}")]
    public async Task<IActionResult> DeleteMemberAsync(
        [FromRoute] string organizationId,
        [FromRoute] string groupId,
        [FromRoute] string memberAccountId,
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(memberAccountId))
        {
            return BadRequest();
        }

        return await ProxyAsync(
            organizationId,
            groupId,
            (accountId, token) => backend.DeleteOrganizationGroupMemberAsync(
                accountId,
                organizationId,
                groupId,
                memberAccountId,
                token),
            cancellationToken);
    }

    private async Task<IActionResult> ProxyAsync(
        string organizationId,
        Func<string, CancellationToken, Task<BackendResponse>> request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(organizationId))
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

        return FromBackend(await request(accountId, cancellationToken));
    }

    private Task<IActionResult> ProxyAsync(
        string organizationId,
        string groupId,
        Func<string, CancellationToken, Task<BackendResponse>> request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(groupId))
        {
            return Task.FromResult<IActionResult>(BadRequest());
        }

        return ProxyAsync(organizationId, request, cancellationToken);
    }
}
