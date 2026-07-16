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

        var ownerId = await GetOwnerIdAsync(cancellationToken);
        if (ownerId is null)
        {
            return Unauthorized();
        }

        var response = await backend.CreateApplicationAsync(ownerId, form, cancellationToken);
        return FromBackend(response);
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var ownerId = await GetOwnerIdAsync(cancellationToken);
        if (ownerId is null)
        {
            return Unauthorized();
        }

        var response = await backend.GetOwnedApplicationsAsync(ownerId, cancellationToken);
        return FromBackend(response);
    }

    private async Task<string?> GetOwnerIdAsync(CancellationToken cancellationToken)
    {
        if (!TryGetSessionId(out var sessionId))
        {
            return null;
        }

        var session = await sessions.GetAsync(sessionId, cancellationToken);
        var ownerId = GetStringClaim(session?.Claims, "preferred_username");
        return string.IsNullOrWhiteSpace(ownerId) ? null : ownerId;
    }

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
}
