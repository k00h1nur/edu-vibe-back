using LMS.Application.Features.Attendance;
using LMS.Domain.Enums;
using LMS.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AttendanceController(ISender sender) : ControllerBase
{
    /// <summary>Lists attendance records, optionally filtered. Powers the admin attendance view.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceDto>>>> GetAll(
        [FromQuery] Guid? classId,
        [FromQuery] Guid? sessionId,
        [FromQuery] Guid? studentProfileId,
        [FromQuery] AttendanceStatus? status,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetAttendanceQuery(classId, sessionId, studentProfileId, status), ct);
        return Ok(ApiResponse<IReadOnlyCollection<AttendanceDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("session/{sessionId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceDto>>>> Session(Guid sessionId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetSessionAttendanceQuery(sessionId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<AttendanceDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("student/{studentProfileId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceDto>>>> Student(Guid studentProfileId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentAttendanceQuery(studentProfileId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<AttendanceDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> Mark([FromBody] MarkAttendanceCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<AttendanceDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AttendanceDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> Update(Guid id, [FromBody] UpdateAttendanceCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { AttendanceId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<AttendanceDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AttendanceDto>.Fail(r.Message ?? "Failed"));
    }
}