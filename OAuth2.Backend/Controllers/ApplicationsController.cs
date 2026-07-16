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

    private static ApplicationSummary ToSummary(OAuthApplication application) =>
        new()
        {
            Id = application.Id,
            Name = application.Name,
            CreatedAt = application.CreatedAt
        };
}
