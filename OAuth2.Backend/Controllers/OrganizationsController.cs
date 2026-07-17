using Microsoft.AspNetCore.Mvc;
using OAuth2.Data;
using OAuth2.DataTransfer;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/organizations")]
public sealed class OrganizationsController(IOrganizations organizations) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromQuery] string accountId,
        [FromBody] CreateOrganizationForm form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest("query.accountId is missing");
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var membership = await organizations.AddOrganizationAsync(
            form.Id.Trim(),
            form.Name.Trim(),
            accountId,
            cancellationToken);
        if (membership is null)
        {
            return Conflict();
        }

        return Created(
            $"/api/v1/organizations/{Uri.EscapeDataString(membership.Organization.Id)}?accountId={Uri.EscapeDataString(accountId)}",
            ToSummary(membership));
    }

    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] string accountId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest("query.accountId is missing");
        }

        var memberships = await organizations.GetOrganizationsAsync(accountId, cancellationToken);
        return Ok(memberships.Select(ToSummary).ToArray());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string id,
        [FromQuery] string accountId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest();
        }

        var membership = await organizations.GetOrganizationAsync(id, accountId, cancellationToken);
        return membership is null ? NotFound() : Ok(ToSummary(membership));
    }

    private static OrganizationSummary ToSummary(OrganizationMembership membership) => new()
    {
        Id = membership.Organization.Id,
        Name = membership.Organization.Name,
        Role = membership.Role,
        CreatedAt = membership.Organization.CreatedAt
    };
}
