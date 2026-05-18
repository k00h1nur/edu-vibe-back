using LMS.Application.Features.Roles;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/admin/rbac")]
public sealed class RolesController(ISender sender) : ControllerBase
{
    [HttpGet("roles")]
    [PermissionAuthorize("Roles.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoleDto>>>> GetRoles(CancellationToken ct)
    {
        var r = await sender.Send(new GetRolesQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<RoleDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost("roles")]
    [PermissionAuthorize("Roles.Create")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> CreateRole([FromBody] CreateRoleCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success ? Ok(ApiResponse<RoleDto>.Ok(r.Data, r.Message)) : BadRequest(ApiResponse<RoleDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("roles/{id:guid}")]
    [PermissionAuthorize("Roles.Update")]
    public async Task<ActionResult<ApiResponse<RoleDto>>> UpdateRole(Guid id, [FromBody] UpdateRoleCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { RoleId = id }, ct);
        return r.Success ? Ok(ApiResponse<RoleDto>.Ok(r.Data, r.Message)) : BadRequest(ApiResponse<RoleDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("roles/{id:guid}")]
    [PermissionAuthorize("Roles.Delete")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteRole(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new DeleteRoleCommand(id), ct);
        return r.Success ? Ok(ApiResponse<object>.Ok(new { }, r.Message)) : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }

    [HttpGet("permissions")]
    [PermissionAuthorize("Permissions.Read")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PermissionDto>>>> GetPermissions(CancellationToken ct)
    {
        var r = await sender.Send(new GetPermissionsQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<PermissionDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost("permissions")]
    [PermissionAuthorize("Permissions.Create")]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> CreatePermission([FromBody] CreatePermissionCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success ? Ok(ApiResponse<PermissionDto>.Ok(r.Data, r.Message)) : BadRequest(ApiResponse<PermissionDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("permissions/{id:guid}")]
    [PermissionAuthorize("Permissions.Update")]
    public async Task<ActionResult<ApiResponse<PermissionDto>>> UpdatePermission(Guid id, [FromBody] UpdatePermissionCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { PermissionId = id }, ct);
        return r.Success ? Ok(ApiResponse<PermissionDto>.Ok(r.Data, r.Message)) : BadRequest(ApiResponse<PermissionDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("permissions/{id:guid}")]
    [PermissionAuthorize("Permissions.Delete")]
    public async Task<ActionResult<ApiResponse<object>>> DeletePermission(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new DeletePermissionCommand(id), ct);
        return r.Success ? Ok(ApiResponse<object>.Ok(new { }, r.Message)) : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("roles/{id:guid}/permissions")]
    [PermissionAuthorize("Roles.AssignPermissions")]
    public async Task<ActionResult<ApiResponse<object>>> AssignPermissions(Guid id, [FromBody] IReadOnlyCollection<Guid> permissionIds, CancellationToken ct)
    {
        var r = await sender.Send(new AssignRolePermissionsCommand(id, permissionIds), ct);
        return r.Success ? Ok(ApiResponse<object>.Ok(new { }, r.Message)) : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}
