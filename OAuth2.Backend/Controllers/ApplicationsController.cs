using Microsoft.AspNetCore.Mvc;
using OAuth2.Data;
using OAuth2.DataTransfer;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/applications")]
public sealed class ApplicationsController(IApplications applications) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromQuery] string ownerId,
        [FromBody] CreateApplicationForm form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest("query.ownerId is missing");
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var application = await applications.AddApplicationAsync(
            form.ClientId.Trim(),
            ownerId,
            form.Name.Trim(),
            form.ApplicationType,
            cancellationToken);
        if (application is null)
        {
            return Conflict();
        }

        return Created(
            $"/api/v1/applications?ownerId={Uri.EscapeDataString(ownerId)}",
            ToSummary(application));
    }

    [HttpGet]
    public async Task<IActionResult> GetOwnedApplicationsAsync(
        [FromQuery] string ownerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest();
        }

        var ownedApplications = await applications.GetOwnedApplicationsAsync(
            ownerId,
            cancellationToken);
        return Ok(ownedApplications
            .Select(ToSummary)
            .ToArray());
    }

    [HttpGet("{**id}")]
    public async Task<IActionResult> GetOwnedApplicationAsync(
        [FromRoute] string id,
        [FromQuery] string ownerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest();
        }

        var configuration = await applications.GetOwnedApplicationAsync(
            id,
            ownerId,
            cancellationToken);
        return configuration is null ? NotFound() : Ok(ToDetails(configuration));
    }

    [HttpPut("{**id}")]
    public async Task<IActionResult> UpdateAsync(
        [FromRoute] string id,
        [FromQuery] string ownerId,
        [FromBody] UpdateApplicationForm form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest();
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var updated = await applications.UpdateApplicationAsync(
            id,
            ownerId,
            form.RedirectUris.Select(static value => value.Trim()).ToArray(),
            form.AllowedScopes.Select(static value => value.Trim()).ToArray(),
            cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    [HttpDelete("{**id}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string id,
        [FromQuery] string ownerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest();
        }

        return await applications.DeleteApplicationAsync(id, ownerId, cancellationToken)
            ? NoContent()
            : NotFound();
    }

    private static ApplicationSummary ToSummary(OAuthApplication application) =>
        new()
        {
            Id = application.Id,
            Name = application.Name,
            ApplicationType = application.ApplicationType,
            CreatedAt = application.CreatedAt
        };

    private static ApplicationDetails ToDetails(OAuthApplicationConfiguration configuration) =>
        new()
        {
            Id = configuration.Application.Id,
            Name = configuration.Application.Name,
            ApplicationType = configuration.Application.ApplicationType,
            CreatedAt = configuration.Application.CreatedAt,
            RedirectUris = configuration.RedirectUris,
            AllowedScopes = configuration.AllowedScopes
        };
}
