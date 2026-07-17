using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
        if (!TryGetSessionId(out var sessionId))
        {
            return new(OwnerResolutionStatus.Unauthorized, null);
        }

        var session = await sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return new(OwnerResolutionStatus.Unauthorized, null);
        }

        if (organizationId is null)
        {
            var ownerId = GetStringClaim(session.Claims, "preferred_username");
            return string.IsNullOrWhiteSpace(ownerId)
                ? new(OwnerResolutionStatus.Unauthorized, null)
                : new(OwnerResolutionStatus.Success, ownerId);
        }

        if (string.IsNullOrWhiteSpace(organizationId)
            || !HasOrganizationMembership(session.Claims, organizationId))
        {
            return new(OwnerResolutionStatus.Forbidden, null);
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
        _ => throw new ArgumentOutOfRangeException(nameof(owner))
    };

    private bool TryGetSessionId(out string sessionId)
    {
        if (Request.Cookies.TryGetValue(sessionOptions.Value.CookieName, out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            sessionId = value;
            return true;
        }

        sessionId = string.Empty;
        return false;
    }

    private static string? GetStringClaim(
        IReadOnlyDictionary<string, JsonElement>? claims,
        string name)
    {
        if (claims is null
            || !claims.TryGetValue(name, out var value)
            || value.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return value.GetString();
    }

    private static bool HasOrganizationMembership(
        IReadOnlyDictionary<string, JsonElement> claims,
        string organizationId)
    {
        if (!claims.TryGetValue("groups", out var groups)
            || groups.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        return groups.EnumerateArray().Any(group =>
            group.ValueKind == JsonValueKind.String
            && string.Equals(group.GetString(), organizationId, StringComparison.Ordinal));
    }

    private static string CreateOrganizationOwnerId(string organizationId)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(organizationId));
        return $"organization:{Convert.ToHexStringLower(hash)}";
    }

    private enum OwnerResolutionStatus
    {
        Success,
        Unauthorized,
        Forbidden
    }

    private readonly record struct OwnerResolution(
        OwnerResolutionStatus Status,
        string? OwnerId);
}
