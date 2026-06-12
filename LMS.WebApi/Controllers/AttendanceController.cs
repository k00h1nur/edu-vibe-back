using LMS.Application.Common.Security;
using LMS.Application.Features.Attendance;
using LMS.Domain.Enums;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
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
    [PermissionAuthorize(Permissions.Attendance.Read)]
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
    [PermissionAuthorize(Permissions.Attendance.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceDto>>>> Session(Guid sessionId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetSessionAttendanceQuery(sessionId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<AttendanceDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>
    /// A student's attendance history. Non-staff callers are self-scoped in
    /// the handler — a student can only read their own record, not another's.
    /// </summary>
    [HttpGet("student/{studentProfileId:guid}")]
    [PermissionAuthorize(Permissions.Attendance.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AttendanceDto>>>> Student(Guid studentProfileId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentAttendanceQuery(studentProfileId), ct);
        if (!r.Success)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<IReadOnlyCollection<AttendanceDto>>.Fail(r.Message ?? "Forbidden"));
        return Ok(ApiResponse<IReadOnlyCollection<AttendanceDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>
    /// The caller's own attendance history, enriched with class title +
    /// session date. Self-scoped from the JWT — gated by Attendance.Read,
    /// which students hold, and never exposes another student's record.
    /// </summary>
    [HttpGet("mine")]
    [PermissionAuthorize(Permissions.Attendance.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MyAttendanceDto>>>> Mine(CancellationToken ct)
    {
        var r = await sender.Send(new GetMyAttendanceQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<MyAttendanceDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Attendance.Mark)]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> Mark([FromBody] MarkAttendanceCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<AttendanceDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AttendanceDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Attendance.Update)]
    public async Task<ActionResult<ApiResponse<AttendanceDto>>> Update(Guid id, [FromBody] UpdateAttendanceCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { AttendanceId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<AttendanceDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AttendanceDto>.Fail(r.Message ?? "Failed"));
    }
}
