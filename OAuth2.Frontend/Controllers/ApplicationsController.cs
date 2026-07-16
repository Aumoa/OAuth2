using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
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
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        if (!TryGetSessionId(out var sessionId))
        {
            return Unauthorized();
        }

        var session = await sessions.GetAsync(sessionId, cancellationToken);
        var ownerId = GetStringClaim(session?.Claims, "preferred_username");
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return Unauthorized();
        }

        var response = await backend.GetOwnedApplicationsAsync(ownerId, cancellationToken);
        return FromBackend(response);
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
