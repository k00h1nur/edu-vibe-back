using LMS.Application.Common.Security;
using LMS.Application.Features.Staff;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class StaffController(ISender sender) : ControllerBase
{
    [HttpGet]
    [PermissionAuthorize(Permissions.Staff.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<StaffDto>>>> GetAll(CancellationToken ct)
    {
        var r = await sender.Send(new GetStaffQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<StaffDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>Returns the staff profile linked to the authenticated user. No extra permission required.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<StaffDto>>> GetMine(CancellationToken ct)
    {
        var r = await sender.Send(new GetMyStaffProfileQuery(), ct);
        return r.Success
            ? Ok(ApiResponse<StaffDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<StaffDto>.Fail(r.Message ?? "Not found"));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Staff.Create)]
    public async Task<ActionResult<ApiResponse<StaffDto>>> Create([FromBody] CreateStaffCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<StaffDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<StaffDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Staff.Update)]
    public async Task<ActionResult<ApiResponse<StaffDto>>> Update(Guid id, [FromBody] UpdateStaffProfileCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { StaffProfileId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<StaffDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<StaffDto>.Fail(r.Message ?? "Failed"));
    }
}
