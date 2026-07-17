using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.Data;
using OAuth2.DataTransfer;
using OAuth2.Services;
using BffSessionOptions = OAuth2.Options.SessionOptions;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/accounts")]
public sealed class AccountsController(
    ISessionsRepository sessions,
    IBackendClient backend,
    IOptions<BffSessionOptions> sessionOptions) : BackendProxyControllerBase
{
    private sealed record CurrentAccount(string SessionId, string Id);

    [HttpHead("{id}")]
    public async Task<IActionResult> ExistsAsync(
        [FromRoute] string id,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        return await backend.AccountExistsAsync(id, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] RegisterForm form,
        CancellationToken cancellationToken)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var response = await backend.RegisterAccountAsync(
            form,
            Request.Headers.AcceptLanguage.ToString(),
            cancellationToken);
        if (response.StatusCode == HttpStatusCode.Created)
        {
            Response.Headers.Location = Url.Action(nameof(ExistsAsync), new { id = form.Id })
                ?? $"/api/v1/accounts/{Uri.EscapeDataString(form.Id)}";
        }

        return FromBackend(response);
    }

    [HttpGet("profile-image")]
    public async Task<IActionResult> GetProfileImageAsync(
        [FromQuery] string id,
        [FromQuery] string? v,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var response = await backend.GetProfileImageAsync(
            id,
            v,
            Request.Headers.IfNoneMatch.ToString(),
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(response.EntityTag))
        {
            Response.Headers.ETag = response.EntityTag;
        }

        if (!string.IsNullOrWhiteSpace(response.CacheControl))
        {
            Response.Headers.CacheControl = response.CacheControl;
        }

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (response.Content is null)
        {
            return StatusCode((int)response.StatusCode);
        }

        return File(response.Content, response.ContentType ?? ProfileImagePolicy.StoredContentType);
    }

    [HttpPut("profile-image")]
    [RequestSizeLimit(ProfileImagePolicy.MaxUploadBytes)]
    public async Task<IActionResult> UpdateProfileImageAsync(
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        if (Request.ContentLength is > ProfileImagePolicy.MaxUploadBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        var current = await GetCurrentAccountAsync(cancellationToken);
        if (current is null)
        {
            return Unauthorized();
        }

        var response = await backend.UpdateProfileImageAsync(
            current.Id,
            Request.Body,
            Request.ContentType,
            cancellationToken);
        if (response.StatusCode != HttpStatusCode.OK || response.Value is null)
        {
            return FromBackend(response);
        }

        var picture = JsonSerializer.SerializeToElement(response.Value.Picture);
        if (!await sessions.UpdateActiveAccountClaimAsync(
                current.SessionId,
                "picture",
                picture,
                cancellationToken))
        {
            return Unauthorized();
        }

        return FromBackend(response);
    }

    [HttpDelete("profile-image")]
    public async Task<IActionResult> DeleteProfileImageAsync(
        CancellationToken cancellationToken)
    {
        if (!BrowserActionRequest.IsValid(Request))
        {
            return Forbid();
        }

        var current = await GetCurrentAccountAsync(cancellationToken);
        if (current is null)
        {
            return Unauthorized();
        }

        var response = await backend.DeleteProfileImageAsync(current.Id, cancellationToken);
        if (response.StatusCode != HttpStatusCode.NoContent)
        {
            return FromBackend(response);
        }

        if (!await sessions.UpdateActiveAccountClaimAsync(
                current.SessionId,
                "picture",
                null,
                cancellationToken))
        {
            return Unauthorized();
        }

        return NoContent();
    }

    private async Task<CurrentAccount?> GetCurrentAccountAsync(
        CancellationToken cancellationToken)
    {
        if (!TryGetSessionId(sessionOptions.Value.CookieName, out var sessionId))
        {
            return null;
        }

        var session = await sessions.GetAsync(sessionId, cancellationToken);
        if (session is null
            || !session.Claims.TryGetValue("preferred_username", out var accountIdClaim)
            || accountIdClaim.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(accountIdClaim.GetString()))
        {
            return null;
        }

        return new CurrentAccount(sessionId, accountIdClaim.GetString()!);
    }
}
