using System.Net;
using System.Security.Cryptography;
using System.Text;
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
    IOptions<BffSessionOptions> sessionOptions) : BackendProxyControllerBase
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

        var response = await backend.CreateApplicationAsync(owner.OwnerId!, form, cancellationToken);
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

        var response = await backend.GetOwnedApplicationsAsync(owner.OwnerId!, cancellationToken);
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

        var response = await backend.GetOwnedApplicationAsync(owner.OwnerId!, id, cancellationToken);
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

        var response = await backend.UpdateApplicationAsync(
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

        var response = await backend.DeleteApplicationAsync(owner.OwnerId!, id, cancellationToken);
        return FromBackend(response);
    }

    private async Task<OwnerResolution> ResolveOwnerAsync(
        string? organizationId,
        CancellationToken cancellationToken)
    {
        var accountId = await GetCurrentAccountIdAsync(
            sessions,
            sessionOptions.Value.CookieName,
            cancellationToken);
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return new(OwnerResolutionStatus.Unauthorized, null);
        }

        if (organizationId is null)
        {
            return new(OwnerResolutionStatus.Success, accountId);
        }

        if (string.IsNullOrWhiteSpace(organizationId))
        {
            return new(OwnerResolutionStatus.Forbidden, null);
        }

        var organization = await backend.GetOrganizationAsync(
            accountId,
            organizationId,
            cancellationToken);
        if (organization.StatusCode == HttpStatusCode.NotFound)
        {
            return new(OwnerResolutionStatus.Forbidden, null);
        }

        if (organization.StatusCode is < HttpStatusCode.OK or >= HttpStatusCode.MultipleChoices)
        {
            return new(OwnerResolutionStatus.BackendFailure, null, organization);
        }

        return new(
            OwnerResolutionStatus.Success,
            CreateOrganizationOwnerId(organizationId));
    }

    private IActionResult? OwnerResolutionError(OwnerResolution owner) => owner.Status switch
    {
        OwnerResolutionStatus.Success => null,
        OwnerResolutionStatus.Unauthorized => Unauthorized(),
        OwnerResolutionStatus.Forbidden => Forbid(),
        OwnerResolutionStatus.BackendFailure => FromBackend(owner.BackendResponse!),
        _ => throw new ArgumentOutOfRangeException(nameof(owner))
    };

    private static string CreateOrganizationOwnerId(string organizationId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(organizationId));
        return $"organization:{Convert.ToHexStringLower(hash)}";
    }

    private enum OwnerResolutionStatus
    {
        Success,
        Unauthorized,
        Forbidden,
        BackendFailure
    }

    private readonly record struct OwnerResolution(
        OwnerResolutionStatus Status,
        string? OwnerId,
        BackendResponse? BackendResponse = null);
}
