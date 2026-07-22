using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using OAuth2.Data;
using OAuth2.DataTransfer;
using OAuth2.OpenId;
using OAuth2.Repositories;
using OAuth2.Services;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/accounts")]
public sealed class AccountsController(
    IAccounts accounts,
    IAccountProfiles accountProfiles,
    IAccountProfileImages profileImages,
    ProfileImageProcessor profileImageProcessor,
    IEmailVerify emailVerify,
    IOptions<OidcProviderOptions> oidcOptions) : ControllerBase
{
    [HttpHead("{id}")]
    public async Task<IActionResult> ExistsAsync(
        [FromRoute] string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var account = await accounts.GetAccountAsync(id, cancellationToken);
        return account is null ? NotFound() : NoContent();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] RegisterForm form,
        CancellationToken cancellationToken = default)
    {
        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var registration = await accounts.AddAccountAsync(
            form.Id,
            form.Password,
            form.FullName,
            form.Email,
            cancellationToken);
        if (registration is null)
        {
            return Conflict();
        }

        await emailVerify.SendAsync(
            registration.Sub,
            registration.VerifyCode,
            new MailAddress(form.Email),
            cancellationToken);

        return CreatedAtAction(
            nameof(ExistsAsync),
            new { id = form.Id },
            new EmailVerificationChallenge
            {
                Sub = registration.Sub
        });
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfileAsync(
        [FromQuery] string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var profile = await accountProfiles.GetAsync(id, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfileAsync(
        [FromQuery] string id,
        [FromBody] UpdateAccountProfileForm form,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var profile = await accountProfiles.UpdateAsync(id, form, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("profile-image")]
    public async Task<IActionResult> GetProfileImageAsync(
        [FromQuery] string id,
        [FromQuery] string? v,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        var image = await profileImages.GetAsync(id, cancellationToken);
        if (image is null)
        {
            Response.Headers.CacheControl = "no-store";
            return NotFound();
        }

        Response.Headers.ETag = image.EntityTag;
        Response.Headers.CacheControl = string.Equals(v, image.Version, StringComparison.Ordinal)
            ? "public,max-age=31536000,immutable"
            : "public,no-cache";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        var ifNoneMatch = Request.Headers.IfNoneMatch.ToString();
        if (ifNoneMatch.Split(',').Any(value =>
                value.Trim() is "*" || string.Equals(
                    value.Trim(),
                    image.EntityTag,
                    StringComparison.Ordinal)))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return File(image.Data, image.ContentType);
    }

    [HttpPut("profile-image")]
    [RequestSizeLimit(ProfileImagePolicy.MaxUploadBytes)]
    public async Task<IActionResult> UpdateProfileImageAsync(
        [FromQuery] string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        if (Request.ContentLength is > ProfileImagePolicy.MaxUploadBytes)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge);
        }

        ProcessedProfileImage image;
        try
        {
            image = await profileImageProcessor.ProcessAsync(Request.Body, cancellationToken);
        }
        catch (ProfileImageProcessingException exception)
        {
            return BadRequest(new
            {
                error = "invalid_profile_image",
                description = exception.Message
            });
        }

        if (!await profileImages.UpsertAsync(id, image, cancellationToken))
        {
            return NotFound();
        }

        return Ok(new ProfileImageReference
        {
            Picture = OidcEndpointUris.ProfileImage(
                oidcOptions.Value.Issuer,
                id,
                image.Version),
            Width = image.Width,
            Height = image.Height
        });
    }

    [HttpDelete("profile-image")]
    public async Task<IActionResult> DeleteProfileImageAsync(
        [FromQuery] string id,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest();
        }

        return await profileImages.DeleteAsync(id, cancellationToken)
            ? NoContent()
            : NotFound();
    }
}
