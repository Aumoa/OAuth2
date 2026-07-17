using Microsoft.AspNetCore.Mvc;
using OAuth2.Data;
using OAuth2.DataTransfer;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/organizations")]
public sealed class OrganizationsController(
    IOrganizations organizations,
    IOrganizationMembers organizationMembers) : ControllerBase
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

    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembersAsync(
        [FromRoute] string id,
        [FromQuery] string accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = OrganizationMemberPage.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(accountId)
            || page <= 0
            || pageSize <= 0
            || pageSize > OrganizationMemberPage.MaxPageSize)
        {
            return BadRequest();
        }

        var result = await organizationMembers.GetPageAsync(
            id,
            accountId,
            page,
            pageSize,
            cancellationToken);
        if (!result.HasValue)
        {
            return NotFound();
        }

        return Ok(new OrganizationMemberPage
        {
            Items = result.Value.Items.Select(ToSummary).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = result.Value.TotalCount
        });
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMemberAsync(
        [FromRoute] string id,
        [FromQuery] string accountId,
        [FromBody] AddOrganizationMemberForm form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest();
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var status = await organizationMembers.AddAsync(
            id,
            accountId,
            form.AccountId.Trim(),
            form.Role,
            cancellationToken);
        return MutationResult(status);
    }

    [HttpPut("{id}/members/{memberAccountId}")]
    public async Task<IActionResult> UpdateMemberAsync(
        [FromRoute] string id,
        [FromRoute] string memberAccountId,
        [FromQuery] string accountId,
        [FromBody] UpdateOrganizationMemberForm form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(memberAccountId)
            || string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest();
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var status = await organizationMembers.UpdateRoleAsync(
            id,
            accountId,
            memberAccountId,
            form.Role,
            cancellationToken);
        return MutationResult(status);
    }

    [HttpDelete("{id}/members/{memberAccountId}")]
    public async Task<IActionResult> DeleteMemberAsync(
        [FromRoute] string id,
        [FromRoute] string memberAccountId,
        [FromQuery] string accountId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id)
            || string.IsNullOrWhiteSpace(memberAccountId)
            || string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest();
        }

        var status = await organizationMembers.DeleteAsync(
            id,
            accountId,
            memberAccountId,
            cancellationToken);
        return MutationResult(status);
    }

    [HttpPut("{id}/owner")]
    public async Task<IActionResult> TransferOwnershipAsync(
        [FromRoute] string id,
        [FromQuery] string accountId,
        [FromBody] TransferOrganizationOwnershipForm form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest();
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var status = await organizationMembers.TransferOwnershipAsync(
            id,
            accountId,
            form.AccountId.Trim(),
            cancellationToken);
        return MutationResult(status);
    }

    private static OrganizationSummary ToSummary(OrganizationMembership membership) => new()
    {
        Id = membership.Organization.Id,
        Name = membership.Organization.Name,
        Role = membership.Role,
        CreatedAt = membership.Organization.CreatedAt
    };

    private static OrganizationMemberSummary ToSummary(OrganizationMember member) => new()
    {
        AccountId = member.AccountId,
        Name = member.Name,
        Role = member.Role,
        JoinedAt = member.JoinedAt
    };

    private IActionResult MutationResult(OrganizationMemberMutationStatus status) => status switch
    {
        OrganizationMemberMutationStatus.Succeeded => NoContent(),
        OrganizationMemberMutationStatus.OrganizationNotFound => NotFound(new
        {
            error = "organization_not_found"
        }),
        OrganizationMemberMutationStatus.AccountNotFound => NotFound(new
        {
            error = "account_not_found"
        }),
        OrganizationMemberMutationStatus.MemberNotFound => NotFound(new
        {
            error = "member_not_found"
        }),
        OrganizationMemberMutationStatus.AlreadyMember => Conflict(new
        {
            error = "member_exists"
        }),
        OrganizationMemberMutationStatus.Forbidden => StatusCode(
            StatusCodes.Status403Forbidden,
            new { error = "insufficient_organization_role" }),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
