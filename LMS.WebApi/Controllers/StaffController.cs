using LMS.Application.Features.Staff;
using LMS.WebApi.Common;
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
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<StaffDto>>>> GetAll(CancellationToken ct)
    {
        var r = await sender.Send(new GetStaffQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<StaffDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StaffDto>>> Create([FromBody] CreateStaffCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<StaffDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<StaffDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StaffDto>>> Update(Guid id, [FromBody] UpdateStaffProfileCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { StaffProfileId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<StaffDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<StaffDto>.Fail(r.Message ?? "Failed"));
    }
}