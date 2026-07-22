using Microsoft.AspNetCore.Mvc;
using OAuth2.Data;
using OAuth2.DataTransfer;
using OAuth2.Repositories;

namespace OAuth2.Controllers;

[ApiController]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[Route("api/v1/applications/{clientId}/roles")]
public sealed class ApplicationRolesController(IApplicationRoles applicationRoles) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string clientId,
        [FromQuery] string ownerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest();
        }

        var roles = await applicationRoles.GetRolesAsync(
            clientId,
            ownerId,
            cancellationToken);
        return roles is null
            ? NotFound()
            : Ok(roles.Select(static entry => ToSummary(entry.Role, entry.MemberCount)).ToArray());
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromRoute] string clientId,
        [FromQuery] string ownerId,
        [FromBody] CreateApplicationRoleForm form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest();
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var result = await applicationRoles.AddRoleAsync(
            clientId,
            ownerId,
            form.Id.Trim(),
            form.Name.Trim(),
            cancellationToken);
        return result.Status switch
        {
            ApplicationRoleMutationStatus.Succeeded => Created(
                $"/api/v1/applications/{Uri.EscapeDataString(clientId)}/roles/{Uri.EscapeDataString(result.Role!.Id)}",
                ToSummary(result.Role, 0)),
            ApplicationRoleMutationStatus.ApplicationNotFound => NotFound(),
            ApplicationRoleMutationStatus.RoleExists => Conflict(),
            ApplicationRoleMutationStatus.RoleLimitReached => Conflict(
                "The application role limit has been reached."),
            _ => throw new InvalidOperationException(
                $"Unexpected application role mutation status: {result.Status}")
        };
    }

    [HttpDelete("{roleId}")]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] string clientId,
        [FromRoute] string roleId,
        [FromQuery] string ownerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(roleId)
            || string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest();
        }

        return MutationResult(await applicationRoles.DeleteRoleAsync(
            clientId,
            ownerId,
            roleId,
            cancellationToken));
    }

    [HttpGet("{roleId}/members")]
    public async Task<IActionResult> GetMembersAsync(
        [FromRoute] string clientId,
        [FromRoute] string roleId,
        [FromQuery] string ownerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = ApplicationRoleMemberPage.DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(roleId)
            || string.IsNullOrWhiteSpace(ownerId)
            || page <= 0
            || pageSize <= 0
            || pageSize > ApplicationRoleMemberPage.MaxPageSize)
        {
            return BadRequest();
        }

        var result = await applicationRoles.GetMembersAsync(
            clientId,
            ownerId,
            roleId,
            page,
            pageSize,
            cancellationToken);
        if (!result.HasValue)
        {
            return NotFound();
        }

        return Ok(new ApplicationRoleMemberPage
        {
            Items = result.Value.Items.Select(ToSummary).ToArray(),
            Page = page,
            PageSize = pageSize,
            TotalCount = result.Value.TotalCount
        });
    }

    [HttpPost("{roleId}/members")]
    public async Task<IActionResult> AddMemberAsync(
        [FromRoute] string clientId,
        [FromRoute] string roleId,
        [FromQuery] string ownerId,
        [FromBody] AddApplicationRoleMemberForm form,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(roleId)
            || string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest();
        }

        if (!form.Verify(out var error))
        {
            return BadRequest(error);
        }

        var status = await applicationRoles.AddMemberAsync(
            clientId,
            ownerId,
            roleId,
            form.AccountId.Trim(),
            cancellationToken);
        return status switch
        {
            ApplicationRoleMutationStatus.Succeeded => NoContent(),
            ApplicationRoleMutationStatus.AlreadyAssigned => Conflict(),
            ApplicationRoleMutationStatus.ApplicationNotFound
                or ApplicationRoleMutationStatus.RoleNotFound
                or ApplicationRoleMutationStatus.AccountNotFound => NotFound(),
            _ => throw new InvalidOperationException(
                $"Unexpected application role mutation status: {status}")
        };
    }

    [HttpDelete("{roleId}/members/{accountId}")]
    public async Task<IActionResult> DeleteMemberAsync(
        [FromRoute] string clientId,
        [FromRoute] string roleId,
        [FromRoute] string accountId,
        [FromQuery] string ownerId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(clientId)
            || string.IsNullOrWhiteSpace(roleId)
            || string.IsNullOrWhiteSpace(accountId)
            || string.IsNullOrWhiteSpace(ownerId))
        {
            return BadRequest();
        }

        return MutationResult(await applicationRoles.DeleteMemberAsync(
            clientId,
            ownerId,
            roleId,
            accountId,
            cancellationToken));
    }

    private IActionResult MutationResult(ApplicationRoleMutationStatus status) => status switch
    {
        ApplicationRoleMutationStatus.Succeeded => NoContent(),
        ApplicationRoleMutationStatus.ApplicationNotFound
            or ApplicationRoleMutationStatus.RoleNotFound
            or ApplicationRoleMutationStatus.AccountNotFound => NotFound(),
        _ => throw new InvalidOperationException(
            $"Unexpected application role mutation status: {status}")
    };

    private static ApplicationRoleSummary ToSummary(
        OAuthApplicationRole role,
        long memberCount) => new()
    {
        Id = role.Id,
        Name = role.Name,
        MemberCount = memberCount,
        CreatedAt = role.CreatedAt
    };

    private static ApplicationRoleMemberSummary ToSummary(
        OAuthApplicationRoleMember member) => new()
    {
        AccountId = member.AccountId,
        Name = member.Name,
        Email = member.Email,
        AssignedAt = member.AssignedAt
    };
}
