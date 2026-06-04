using LMS.Application.Common.Models;
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
    public async Task<ActionResult<ApiResponse<PagedResult<StaffDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var r = await sender.Send(new GetStaffQuery(page, pageSize, search), ct);
        return Ok(ApiResponse<PagedResult<StaffDto>>.Ok(r.Data, r.Message));
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

    /// <summary>
    /// Updates the editable staff profile fields: first/last name, phone, description.
    /// Employment type is updated separately via the main PUT endpoint.
    /// </summary>
    [HttpPut("{id:guid}/details")]
    [PermissionAuthorize(Permissions.Staff.Update)]
    public async Task<ActionResult<ApiResponse<StaffDto>>> UpdateDetails(Guid id,
        [FromBody] UpdateStaffDetailsCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { StaffProfileId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<StaffDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<StaffDto>.Fail(r.Message ?? "Failed"));
    }
}
