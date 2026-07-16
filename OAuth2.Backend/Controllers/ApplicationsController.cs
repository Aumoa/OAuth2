using Microsoft.AspNetCore.Mvc;
using OAuth2.DataTransfer;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/applications")]
public sealed class ApplicationsController(IApplications applications) : ControllerBase
{
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
            .Select(static application => new ApplicationSummary
            {
                Id = application.Id,
                Name = application.Name,
                CreatedAt = application.CreatedAt
            })
            .ToArray());
    }
}
