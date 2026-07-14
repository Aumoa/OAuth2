using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.OpenId;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/session")]
public sealed class SessionsController(
    ISessionsRepository sessions,
    IOptions<BffSessionOptions> sessionOptions) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        if (!TryGetSessionId(out var sessionId))
        {
            return Unauthorized();
        }

        var session = await sessions.GetAsync(sessionId, cancellationToken);
        if (session is null
            || !OidcScopePolicy.TryCombine(
                session.GrantedScope,
                session.SessionScope,
                out var effectiveScope))
        {
            await InvalidateAsync(sessionId, cancellationToken);
            return Unauthorized();
        }

        var claims = OidcClaimPolicy.Filter(session.Claims, effectiveScope);
        if (!claims.ContainsKey("sub"))
        {
            await InvalidateAsync(sessionId, cancellationToken);
            return Unauthorized();
        }

        return Ok(claims);
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync(CancellationToken cancellationToken)
    {
        if (TryGetSessionId(out var sessionId))
        {
            await sessions.DeleteAsync(sessionId, cancellationToken);
        }

        DeleteSessionCookie();
        return NoContent();
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

    private async Task InvalidateAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        await sessions.DeleteAsync(sessionId, cancellationToken);
        DeleteSessionCookie();
    }

    private void DeleteSessionCookie()
    {
        Response.Cookies.Delete(
            sessionOptions.Value.CookieName,
            SessionCookieOptionsFactory.CreateDelete());
    }
}
