using Microsoft.AspNetCore.Mvc;
using OAuth2.Services;

namespace OAuth2.Controllers;

public abstract class BackendProxyControllerBase : ControllerBase
{
    protected async Task<string?> GetCurrentAccountIdAsync(
        ISessionsRepository sessions,
        string cookieName,
        CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(cookieName, out var sessionId)
            || string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        var session = await sessions.GetAsync(sessionId, cancellationToken);
        if (session is null
            || !session.Claims.TryGetValue("preferred_username", out var accountIdClaim)
            || accountIdClaim.ValueKind != System.Text.Json.JsonValueKind.String)
        {
            return null;
        }

        return accountIdClaim.GetString();
    }

    protected IActionResult FromBackend(BackendResponse response)
    {
        if (string.IsNullOrEmpty(response.Content))
        {
            return StatusCode((int)response.StatusCode);
        }

        return new ContentResult
        {
            Content = response.Content,
            ContentType = response.ContentType ?? "application/json; charset=utf-8",
            StatusCode = (int)response.StatusCode
        };
    }
}
