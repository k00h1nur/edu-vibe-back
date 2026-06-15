using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Analytics;
using LMS.Application.Features.Classes;
using LMS.Application.Features.Sessions;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ClassesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [PermissionAuthorize(Permissions.Classes.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<ClassDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var r = await sender.Send(new GetClassesQuery(page, pageSize, search), ct);
        return Ok(ApiResponse<PagedResult<ClassDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}")]
    [PermissionAuthorize(Permissions.Classes.Read)]
    public async Task<ActionResult<ApiResponse<ClassDto>>> Get(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetClassByIdQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<ClassDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<ClassDto>.Fail(r.Message ?? "Not found"));
    }

    [HttpGet("assigned/{teacherUserId:guid}")]
    [PermissionAuthorize(Permissions.Classes.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ClassDto>>>> Assigned(Guid teacherUserId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetAssignedClassesQuery(teacherUserId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<ClassDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>
    /// The caller's enrolled classes (student self-view) — teacher name and
    /// next session joined in. Self-scoped from the JWT, so no Classes.Read
    /// is required; students can open their own classes without the admin
    /// read permission.
    /// </summary>
    [HttpGet("mine")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MyClassDto>>>> Mine(CancellationToken ct)
    {
        var r = await sender.Send(new GetMyClassesQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<MyClassDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}/students")]
    [PermissionAuthorize(Permissions.Classes.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<Guid>>>> Students(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetClassStudentsQuery(id), ct);
        return Ok(ApiResponse<IReadOnlyCollection<Guid>>.Ok(r.Data, r.Message));
    }

    /// <summary>
    /// Class engagement + outcomes: attendance rate, average grade, assignment +
    /// lesson completion rates, and at-risk students. Self-scoped in the handler
    /// to the class's own teacher or staff — no cross-class access.
    /// </summary>
    [HttpGet("{id:guid}/analytics")]
    public async Task<ActionResult<ApiResponse<ClassAnalyticsDto>>> Analytics(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetClassAnalyticsQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<ClassAnalyticsDto>.Ok(r.Data, r.Message))
            : r.ErrorCode == "FORBIDDEN"
                ? StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ClassAnalyticsDto>.Fail(r.Message ?? "Forbidden"))
                : NotFound(ApiResponse<ClassAnalyticsDto>.Fail(r.Message ?? "Not found"));
    }

    /// <summary>The class's recurring schedule pattern; 404 until one is set.</summary>
    [HttpGet("{id:guid}/schedule-pattern")]
    [PermissionAuthorize(Permissions.Sessions.Read)]
    public async Task<ActionResult<ApiResponse<SchedulePatternDto>>> GetSchedulePattern(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetClassSchedulePatternQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<SchedulePatternDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<SchedulePatternDto>.Fail(r.Message ?? "Not found"));
    }

    /// <summary>
    /// Upserts the recurring pattern and (re)generates the class's lesson
    /// sessions from it. Past lessons and lessons that already have
    /// attendance marks are never touched — only future, unmarked lessons
    /// are replaced. This is the bulk alternative to creating sessions one
    /// by one via POST /api/ClassSessions.
    /// </summary>
    [HttpPut("{id:guid}/schedule-pattern")]
    [PermissionAuthorize(Permissions.Sessions.Create)]
    public async Task<ActionResult<ApiResponse<ApplyScheduleResultDto>>> ApplySchedulePattern(
        Guid id, [FromBody] ApplyClassScheduleCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { ClassId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<ApplyScheduleResultDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<ApplyScheduleResultDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Classes.Create)]
    public async Task<ActionResult<ApiResponse<ClassDto>>> Create([FromBody] CreateClassCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<ClassDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<ClassDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Classes.Update)]
    public async Task<ActionResult<ApiResponse<ClassDto>>> Update(Guid id, [FromBody] UpdateClassCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { ClassId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<ClassDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<ClassDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(Permissions.Classes.Delete)]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new CancelClassCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/enroll/{studentProfileId:guid}")]
    [PermissionAuthorize(Permissions.Classes.Enroll)]
    public async Task<ActionResult<ApiResponse<object>>> Enroll(Guid id, Guid studentProfileId, CancellationToken ct)
    {
        var r = await sender.Send(new EnrollStudentCommand(id, studentProfileId), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}/enroll/{studentProfileId:guid}")]
    [PermissionAuthorize(Permissions.Classes.Enroll)]
    public async Task<ActionResult<ApiResponse<object>>> Drop(Guid id, Guid studentProfileId, CancellationToken ct)
    {
        var r = await sender.Send(new RemoveStudentFromClassCommand(id, studentProfileId), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}
