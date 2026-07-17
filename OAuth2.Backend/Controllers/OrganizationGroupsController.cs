using Microsoft.AspNetCore.Mvc;
using OAuth2.Data;
using OAuth2.DataTransfer;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[Route("api/v1/organizations/{organizationId}/groups")]
public sealed class OrganizationGroupsController(IOrganizationGroups organizationGroups)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string organizationId,
        [FromQuery] string accountId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(organizationId) || string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest();
        }

        var groups = await organizationGroups.GetGroupsAsync(
            organizationId,
            accountId,
            cancellationToken);
        return groups is null
            ? NotFound()
            : Ok(groups.Select(static item => ToSummary(item.Group, item.MemberCount)).ToArray());
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromRoute] string organizationId,
        [FromQuery] string accountId,
        [FromBody] CreateOrganizationGroupForm form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(organizationId) || string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest();
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var result = await organizationGroups.AddGroupAsync(
            organizationId,
            accountId,
            form.Id.Trim(),
            form.Name.Trim(),
            cancellationToken);
        if (result.Status != OrganizationGroupMutationStatus.Succeeded)
        {
            return MutationResult(result.Status);
        }

        var summary = ToSummary(result.Group!, 0);
        return Created(
            $"/api/v1/organizations/{Uri.EscapeDataString(organizationId)}/groups/{Uri.EscapeDataString(summary.Id)}?accountId={Uri.EscapeDataString(accountId)}",
            summary);
    }

    [HttpGet("{groupId}")]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string organizationId,
        [FromRoute] string groupId,
        [FromQuery] string accountId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(organizationId)
            || string.IsNullOrWhiteSpace(groupId)
            || string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest();
        }

        var group = await organizationGroups.GetGroupAsync(
            organizationId,
            groupId,
            accountId,
            cancellationToken);
        return group is null
            ? NotFound()
            : Ok(ToSummary(group.Value.Group, group.Value.MemberCount));
    }

    [HttpGet("{groupId}/members")]
    public async Task<IActionResult> GetMembersAsync(
        [FromRoute] string organizationId,
        [FromRoute] string groupId,
        [FromQuery] string accountId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = OrganizationMemberPage.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(organizationId)
            || string.IsNullOrWhiteSpace(groupId)
            || string.IsNullOrWhiteSpace(accountId)
            || page <= 0
            || pageSize <= 0
            || pageSize > OrganizationMemberPage.MaxPageSize)
        {
            return BadRequest();
        }

        var result = await organizationGroups.GetMembersAsync(
            organizationId,
            groupId,
            accountId,
            page,
            pageSize,
            cancellationToken);
        if (result is null)
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

    [HttpPost("{groupId}/members")]
    public async Task<IActionResult> AddMemberAsync(
        [FromRoute] string organizationId,
        [FromRoute] string groupId,
        [FromQuery] string accountId,
        [FromBody] AddOrganizationGroupMemberForm form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(organizationId)
            || string.IsNullOrWhiteSpace(groupId)
            || string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest();
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var status = await organizationGroups.AddMemberAsync(
            organizationId,
            groupId,
            accountId,
            form.AccountId.Trim(),
            cancellationToken);
        return MutationResult(status);
    }

    [HttpDelete("{groupId}/members/{memberAccountId}")]
    public async Task<IActionResult> DeleteMemberAsync(
        [FromRoute] string organizationId,
        [FromRoute] string groupId,
        [FromRoute] string memberAccountId,
        [FromQuery] string accountId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(organizationId)
            || string.IsNullOrWhiteSpace(groupId)
            || string.IsNullOrWhiteSpace(memberAccountId)
            || string.IsNullOrWhiteSpace(accountId))
        {
            return BadRequest();
        }

        var status = await organizationGroups.DeleteMemberAsync(
            organizationId,
            groupId,
            accountId,
            memberAccountId,
            cancellationToken);
        return MutationResult(status);
    }

    private static OrganizationGroupSummary ToSummary(OrganizationGroup group, long memberCount) => new()
    {
        OrganizationId = group.OrganizationId,
        Id = group.Id,
        Name = group.Name,
        MemberCount = memberCount,
        CreatedAt = group.CreatedAt
    };

    private static OrganizationMemberSummary ToSummary(OrganizationMember member) => new()
    {
        AccountId = member.AccountId,
        Name = member.Name,
        Role = member.Role,
        JoinedAt = member.JoinedAt
    };

    private IActionResult MutationResult(OrganizationGroupMutationStatus status) => status switch
    {
        OrganizationGroupMutationStatus.Succeeded => NoContent(),
        OrganizationGroupMutationStatus.OrganizationNotFound => NotFound(new
        {
            error = "organization_not_found"
        }),
        OrganizationGroupMutationStatus.GroupNotFound => NotFound(new
        {
            error = "organization_group_not_found"
        }),
        OrganizationGroupMutationStatus.MemberNotFound => NotFound(new
        {
            error = "organization_member_not_found"
        }),
        OrganizationGroupMutationStatus.GroupExists => Conflict(new
        {
            error = "organization_group_exists"
        }),
        OrganizationGroupMutationStatus.AlreadyMember => Conflict(new
        {
            error = "organization_group_member_exists"
        }),
        OrganizationGroupMutationStatus.Forbidden => StatusCode(
            StatusCodes.Status403Forbidden,
            new { error = "insufficient_organization_role" }),
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };
}
