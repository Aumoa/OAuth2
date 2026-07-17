using OAuth2.DataTransfer;
using OAuth2.OpenId;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Services;

public sealed class BrowserSessionSignIn(
    ISessionsRepository sessions,
    IBackendClient backend,
    Microsoft.Extensions.Options.IOptions<BffSessionOptions> sessionOptions,
    ILogger<BrowserSessionSignIn> logger)
{
    public async Task SignInAsync(
        HttpContext context,
        GrantedUserInfo userInfo,
        CancellationToken cancellationToken)
    {
        context.Request.Cookies.TryGetValue(
            sessionOptions.Value.CookieName,
            out var currentSessionId);
        var session = await sessions.CreateAsync(
            userInfo,
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

        context.Response.Cookies.Append(
            sessionOptions.Value.CookieName,
            session.Id,
            SessionCookieOptionsFactory.Create(session.ExpiresAt));
    }
}
