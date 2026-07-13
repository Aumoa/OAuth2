using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Options;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    IBackendClient backend,
    ISessionsRepository sessions,
    IOptions<OAuthOptions> oauthOptions,
    IOptions<BffSessionOptions> sessionOptions) : ControllerBase
{
    private const string PkceVerifierCookieName = "oauth2_pkce_verifier";
    private const string PkceStateCookieName = "oauth2_pkce_state";

    [HttpGet("login")]
    public IActionResult StartLogin()
    {
        var codeVerifier = Pkce.CreateCodeVerifier();
        var state = Pkce.CreateCodeVerifier();
        var nonce = Pkce.CreateCodeVerifier();
        var cookieOptions = CreatePkceCookieOptions();

        Response.Cookies.Append(PkceVerifierCookieName, codeVerifier, cookieOptions);
        Response.Cookies.Append(PkceStateCookieName, state, cookieOptions);

        var authorizeUri = QueryHelpers.AddQueryString(
            "/api/v1/auth/authorize",
            new Dictionary<string, string?>
            {
                ["client_id"] = oauthOptions.Value.ClientId,
                ["redirect_uri"] = InternalOidcAuthorization.RedirectUri,
                ["response_type"] = "code",
                ["scope"] = InternalOidcAuthorization.Scope,
                ["state"] = state,
                ["nonce"] = nonce,
                ["code_challenge"] = Pkce.CreateCodeChallenge(codeVerifier),
                ["code_challenge_method"] = "S256"
            });

        return Redirect(authorizeUri);
    }

    [HttpGet("authorize")]
    public IActionResult Authorize(
        [FromQuery(Name = "client_id")] string? clientId,
        [FromQuery(Name = "redirect_uri")] string? redirectUri,
        [FromQuery(Name = "response_type")] string? responseType,
        [FromQuery] string? scope,
        [FromQuery] string? state,
        [FromQuery] string? nonce,
        [FromQuery(Name = "code_challenge")] string? codeChallenge,
        [FromQuery(Name = "code_challenge_method")] string? codeChallengeMethod)
    {
        var authorization = new OidcAuthorizationRequest
        {
            ClientId = clientId,
            RedirectUri = redirectUri,
            ResponseType = responseType,
            Scope = scope,
            State = state,
            Nonce = nonce,
            CodeChallenge = codeChallenge,
            CodeChallengeMethod = codeChallengeMethod
        };

        if (!InternalOidcAuthorization.TryValidate(
            authorization,
            oauthOptions.Value.ClientId,
            out var normalizedScope,
            out var error))
        {
            return BadRequest(new { error });
        }

        return Redirect(QueryHelpers.AddQueryString(
            "/login",
            new Dictionary<string, string?>
            {
                ["client_id"] = authorization.ClientId,
                ["redirect_uri"] = authorization.RedirectUri,
                ["response_type"] = authorization.ResponseType,
                ["scope"] = normalizedScope,
                ["state"] = authorization.State,
                ["nonce"] = authorization.Nonce,
                ["code_challenge"] = authorization.CodeChallenge,
                ["code_challenge_method"] = authorization.CodeChallengeMethod
            }));
    }

    [HttpGet("redirect")]
    public async Task<IActionResult> RedirectAsync(
        [FromQuery] string? code,
        [FromQuery] string? state,
        CancellationToken cancellationToken = default)
    {
        var deleteCookieOptions = CreatePkceDeleteCookieOptions();

        try
        {
            if (string.IsNullOrWhiteSpace(code)
                || !Request.Cookies.TryGetValue(PkceVerifierCookieName, out var codeVerifier)
                || string.IsNullOrWhiteSpace(codeVerifier))
            {
                return BadRequest(new { error = "invalid_grant" });
            }

            if (!Request.Cookies.TryGetValue(PkceStateCookieName, out var expectedState)
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

            var session = await sessions.CreateAsync(response.Value, cancellationToken);
            Response.Cookies.Append(
                sessionOptions.Value.CookieName,
                session.Id,
                CreateSessionCookieOptions(session.ExpiresAt));
            return Redirect("/");
        }
        finally
        {
            Response.Cookies.Delete(PkceVerifierCookieName, deleteCookieOptions);
            Response.Cookies.Delete(PkceStateCookieName, deleteCookieOptions);
        }
    }

    private IActionResult BackendError(BackendResponse<SessionUser> response)
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

    private static CookieOptions CreatePkceCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = InternalOidcAuthorization.RedirectUri,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10)
        };
    }

    private static CookieOptions CreatePkceDeleteCookieOptions()
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = InternalOidcAuthorization.RedirectUri
        };
    }

    private static CookieOptions CreateSessionCookieOptions(DateTimeOffset expiresAt)
    {
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = expiresAt
        };
    }
}
