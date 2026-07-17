using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/session")]
public sealed class SessionsController(
    ISessionsRepository sessions,
    IBackendClient backend,
    IOptions<BffSessionOptions> sessionOptions,
    ILogger<SessionsController> logger) : BackendProxyControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        if (!TryGetSessionId(out var sessionId))
        {
            return Unauthorized();
        }

        var session = await sessions.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return Unauthorized();
        }

        if (!OidcScopePolicy.TryCombine(
            session.GrantedScope,
            session.SessionScope,
            out var effectiveScope))
        {
            await InvalidateActiveSessionAsync(sessionId, cancellationToken);
            return Unauthorized();
        }

        var claims = OidcClaimPolicy.Filter(session.Claims, effectiveScope);
        if (!claims.ContainsKey("sub"))
        {
            await InvalidateActiveSessionAsync(sessionId, cancellationToken);
            return Unauthorized();
        }

        return Ok(claims);
    }

    [HttpGet("accounts")]
    public async Task<IActionResult> GetRememberedAccountsAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetSessionId(out var sessionId))
        {
            return Ok(Array.Empty<RememberedAccount>());
        }

        var accounts = await sessions.GetRememberedAccountsAsync(
            sessionId,
            cancellationToken);
        return Ok(accounts);
    }

    [HttpPost("accounts/{accountKey}/authorization-codes")]
    public async Task<IActionResult> CreateAuthorizationCodeAsync(
        [FromRoute] string accountKey,
        [FromBody] RememberedAuthorizationForm form,
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

        if (!TryGetSessionId(out var sessionId))
        {
            return Unauthorized();
        }

        var credential = await sessions.GetRememberedAccountCredentialAsync(
            sessionId,
            accountKey,
            cancellationToken);
        if (credential is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(credential.Token))
        {
            return Unauthorized(new { error = "reauthentication_required" });
        }

        var response = await backend.CreateAuthorizationCodeFromRememberedSessionAsync(
            new RememberedLoginForm
            {
                Token = credential.Token,
                Authorization = form.Authorization,
                ConsentGranted = form.ConsentGranted
            },
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var removed = await sessions.RemoveRememberedAccountAsync(
                sessionId,
                accountKey,
                cancellationToken);
            if (removed.Found && !removed.HasRemainingAccounts)
            {
                DeleteSessionCookie();
            }
        }

        return FromBackend(response);
    }

    [HttpDelete("accounts/{accountKey}")]
    public async Task<IActionResult> DeleteRememberedAccountAsync(
        [FromRoute] string accountKey,
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (!TryGetSessionId(out var sessionId))
        {
            return NotFound();
        }

        var credential = await sessions.GetRememberedAccountCredentialAsync(
            sessionId,
            accountKey,
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(credential?.Token))
        {
            try
            {
                var response = await backend.RevokeRememberedSessionAsync(
                    credential.Token,
                    cancellationToken);
                if ((int)response.StatusCode >= 400)
                {
                    logger.LogWarning(
                        "Refusing to remove an account because its remembered session could not be revoked. Status: {StatusCode}",
                        (int)response.StatusCode);
                    return StatusCode(StatusCodes.Status502BadGateway);
                }
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception,
                    "Refusing to remove an account because its remembered session could not be revoked.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }

        var removed = await sessions.RemoveRememberedAccountAsync(
            sessionId,
            accountKey,
            cancellationToken);
        if (!removed.Found)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(removed.Token)
            && !string.Equals(removed.Token, credential?.Token, StringComparison.Ordinal))
        {
            try
            {
                var response = await backend.RevokeRememberedSessionAsync(
                    removed.Token,
                    cancellationToken);
                if ((int)response.StatusCode >= 400)
                {
                    logger.LogWarning(
                        "Failed to revoke a removed remembered session. Status: {StatusCode}",
                        (int)response.StatusCode);
                }
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                logger.LogWarning(
                    exception,
                    "Failed to revoke a removed remembered session.");
            }
        }

        if (!removed.HasRemainingAccounts)
        {
            DeleteSessionCookie();
        }

        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAsync(CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (TryGetSessionId(out var sessionId)
            && !await sessions.SignOutAsync(sessionId, cancellationToken))
        {
            DeleteSessionCookie();
        }

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

    private async Task InvalidateActiveSessionAsync(
        string sessionId,
        CancellationToken cancellationToken)
    {
        if (!await sessions.SignOutAsync(sessionId, cancellationToken))
        {
            DeleteSessionCookie();
        }
    }

    private void DeleteSessionCookie()
    {
        Response.Cookies.Delete(
            sessionOptions.Value.CookieName,
            SessionCookieOptionsFactory.CreateDelete());
    }
}
