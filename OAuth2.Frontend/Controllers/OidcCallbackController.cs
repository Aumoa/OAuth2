using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Options;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/auth")]
public sealed class OidcCallbackController(
    IBackendClient backend,
    ISessionsRepository sessions,
    IOptions<OAuthOptions> oauthOptions,
    IOptions<BffSessionOptions> sessionOptions,
    ILogger<OidcCallbackController> logger) : ControllerBase
{
    [HttpGet("callback")]
    public async Task<IActionResult> CallbackAsync(
        [FromQuery] string? code,
        [FromQuery] string? state,
        CancellationToken cancellationToken = default)
    {
        var deleteCookieOptions = OidcFlowCookies.CreateDelete();

        try
        {
            if (string.IsNullOrWhiteSpace(code)
                || !Request.Cookies.TryGetValue(OidcFlowCookies.VerifierName, out var codeVerifier)
                || string.IsNullOrWhiteSpace(codeVerifier))
            {
                return BadRequest(new { error = "invalid_grant" });
            }

            if (!Request.Cookies.TryGetValue(OidcFlowCookies.StateName, out var expectedState)
                || string.IsNullOrWhiteSpace(expectedState)
                || !string.Equals(expectedState, state, StringComparison.Ordinal))
            {
                return BadRequest(new { error = "invalid_state" });
            }

            var response = await backend.ExchangeAuthorizationCodeAsync(
                new AuthorizationCodeExchange
                {
                    Code = code,
                    ClientId = oauthOptions.Value.ClientId,
                    RedirectUri = InternalOidcAuthorization.RedirectUri,
                    CodeVerifier = codeVerifier
                },
                cancellationToken);
            if (response.StatusCode != HttpStatusCode.OK || response.Value is null)
            {
                return BackendError(response);
            }

            if (!OidcScopePolicy.TryCombine(
                    response.Value.Scope,
                    InternalOidcAuthorization.Scope,
                    out _)
                || !response.Value.Claims.ContainsKey("sub"))
            {
                return BadRequest(new { error = "invalid_grant" });
            }

            Request.Cookies.TryGetValue(
                sessionOptions.Value.CookieName,
                out var currentSessionId);
            var session = await sessions.CreateAsync(
                response.Value,
                InternalOidcAuthorization.Scope,
                currentSessionId,
                cancellationToken);
            foreach (var token in session.SupersededRememberedSessionTokens)
            {
                try
                {
                    var revocation = await backend.RevokeRememberedSessionAsync(
                        token,
                        cancellationToken);
                    if ((int)revocation.StatusCode >= 400)
                    {
                        logger.LogWarning(
                            "Failed to revoke a superseded remembered session. Status: {StatusCode}",
                            (int)revocation.StatusCode);
                    }
                }
                catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
                {
                    logger.LogWarning(
                        exception,
                        "Failed to revoke a superseded remembered session.");
                }
            }

            Response.Cookies.Append(
                sessionOptions.Value.CookieName,
                session.Id,
                SessionCookieOptionsFactory.Create(session.ExpiresAt));
            return Redirect("/");
        }
        finally
        {
            Response.Cookies.Delete(OidcFlowCookies.VerifierName, deleteCookieOptions);
            Response.Cookies.Delete(OidcFlowCookies.StateName, deleteCookieOptions);
        }
    }

    private IActionResult BackendError(BackendResponse<GrantedUserInfo> response)
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
