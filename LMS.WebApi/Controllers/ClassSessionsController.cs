using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Analytics;
using LMS.Application.Features.Lessons;
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
public sealed class ClassSessionsController(
    ISender sender,
    ICurrentUserService currentUser,
    ILessonMaterialFileStore lessonFiles) : ControllerBase
{
    private const long LessonUploadSizeLimit = 50 * 1024 * 1024; // 50 MB per lesson file

    public sealed record SetVideoRequest(string? VideoUrl);
    public sealed record CompleteRequest(bool Completed);
    public sealed record SetVisibilityRequest(bool IsPublished, DateTime? VisibleFrom, DateTime? VisibleUntil);
    [HttpGet("class/{classId:guid}")]
    [PermissionAuthorize(Permissions.Sessions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SessionDto>>>> ByClass(Guid classId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetClassSessionsQuery(classId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<SessionDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>
    /// Single-session lookup by id. Powers the per-session attendance marker —
    /// without it the frontend had to walk every class's session list to find
    /// the session by its id.
    /// </summary>
    [HttpGet("{id:guid}")]
    [PermissionAuthorize(Permissions.Sessions.Read)]
    public async Task<ActionResult<ApiResponse<SessionDto>>> Get(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetSessionByIdQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<SessionDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<SessionDto>.Fail(r.Message ?? "Not found"));
    }

    /// <summary>
    /// The caller's own schedule. Self-only — the route id must match the
    /// authenticated user, otherwise 403. The userId is kept as a route param
    /// for wire-compat; new clients can ignore the value and rely on the JWT.
    /// No Sessions.Read permission required: the self-only gate removes the
    /// IDOR surface, and students (who lack Sessions.Read by design — the
    /// admin schedule grid shouldn't be theirs) still need their own timetable.
    /// </summary>
    [HttpGet("my/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ScheduleEntryDto>>>> My(Guid userId, CancellationToken ct)
    {
        if (currentUser.UserId is null || currentUser.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<IReadOnlyCollection<ScheduleEntryDto>>.Fail("Schedule is self-only."));

        var r = await sender.Send(new GetMyScheduleQuery(userId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<ScheduleEntryDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>
    /// Upcoming sessions (today onwards) for the caller, capped by <paramref name="take"/>.
    /// Self-only — same gate (and same no-permission rationale) as <see cref="My"/>.
    /// </summary>
    [HttpGet("upcoming/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ScheduleEntryDto>>>> Upcoming(
        Guid userId, [FromQuery] int take = 20, CancellationToken ct = default)
    {
        if (currentUser.UserId is null || currentUser.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<IReadOnlyCollection<ScheduleEntryDto>>.Fail("Schedule is self-only."));

        var r = await sender.Send(new GetUpcomingSessionsQuery(userId, take), ct);
        return Ok(ApiResponse<IReadOnlyCollection<ScheduleEntryDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>
    /// Admin "today's lessons" entry point — every session scheduled on a
    /// given date across every class. Used by the admin attendance flow to
    /// turn "make attendance" into a one-click list of today's sessions.
    /// </summary>
    [HttpGet("on/{date}")]
    [PermissionAuthorize(Permissions.Sessions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SessionDto>>>> OnDate(
        DateOnly date, [FromQuery] Guid? classId, CancellationToken ct)
    {
        var r = await sender.Send(new GetSessionsForDateQuery(date, classId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<SessionDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>
    /// Schedule view across a date range, joined with the parent class so the
    /// admin schedule grid renders without N+1 lookups. Inclusive on both ends.
    /// </summary>
    [HttpGet("schedule")]
    [PermissionAuthorize(Permissions.Sessions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ScheduleEntryDto>>>> Schedule(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var r = await sender.Send(new GetScheduleQuery(from, to), ct);
        return r.Success
            ? Ok(ApiResponse<IReadOnlyCollection<ScheduleEntryDto>>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<IReadOnlyCollection<ScheduleEntryDto>>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Sessions.Create)]
    public async Task<ActionResult<ApiResponse<SessionDto>>> Create([FromBody] CreateClassSessionCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<SessionDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<SessionDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Sessions.Update)]
    public async Task<ActionResult<ApiResponse<SessionDto>>> Update(Guid id, [FromBody] UpdateClassSessionCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { SessionId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<SessionDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<SessionDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>
    /// Teacher lesson editor — set the topic, online meeting link (Zoom /
    /// Google Meet) and notes on one of the caller's own sessions. Self-scoped
    /// in the handler to the class's teacher, so it needs no Sessions.Update
    /// (admins use PUT instead). FORBIDDEN → 403, NOT_FOUND → 404.
    /// </summary>
    [HttpPost("{id:guid}/details")]
    public async Task<ActionResult<ApiResponse<SessionDto>>> SetDetails(
        Guid id, [FromBody] SetSessionDetailsCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { SessionId = id }, ct);
        if (r.Success) return Ok(ApiResponse<SessionDto>.Ok(r.Data, r.Message));
        return r.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(ApiResponse<SessionDto>.Fail(r.Message ?? "Not found")),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<SessionDto>.Fail(r.Message ?? "Forbidden")),
            _ => BadRequest(ApiResponse<SessionDto>.Fail(r.Message ?? "Failed")),
        };
    }

    // ===== Lesson hub =====================================================
    // Self-scoped in the handlers (class teacher / enrolled student / staff),
    // so these carry no PermissionAuthorize — students lack Sessions.Read but
    // still need to read their own lessons.

    private ActionResult<ApiResponse<T>> MapFail<T>(Result<T> r) => r.ErrorCode switch
    {
        "NOT_FOUND" => NotFound(ApiResponse<T>.Fail(r.Message ?? "Not found")),
        "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<T>.Fail(r.Message ?? "Forbidden")),
        _ => BadRequest(ApiResponse<T>.Fail(r.Message ?? "Failed")),
    };

    /// <summary>The full lesson hub — session + video + materials + assignments + progress.</summary>
    [HttpGet("{id:guid}/full")]
    public async Task<ActionResult<ApiResponse<LessonFullDto>>> Full(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetLessonFullQuery(id), ct);
        return r.Success ? Ok(ApiResponse<LessonFullDto>.Ok(r.Data, r.Message)) : MapFail(r);
    }

    /// <summary>Session attendance roster + each student's status + counts (teacher of the class / staff).</summary>
    [HttpGet("{id:guid}/attendance")]
    public async Task<ActionResult<ApiResponse<SessionAttendanceDto>>> SessionAttendance(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetSessionAttendanceSummaryQuery(id), ct);
        return r.Success ? Ok(ApiResponse<SessionAttendanceDto>.Ok(r.Data, r.Message)) : MapFail(r);
    }

    /// <summary>Bulk-mark attendance for a session — Present/Absent/Late/Excused per student.</summary>
    [HttpPost("{id:guid}/attendance")]
    public async Task<ActionResult<ApiResponse<SessionAttendanceDto>>> BulkAttendance(
        Guid id, [FromBody] BulkMarkAttendanceCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { SessionId = id }, ct);
        return r.Success ? Ok(ApiResponse<SessionAttendanceDto>.Ok(r.Data, r.Message)) : MapFail(r);
    }

    /// <summary>Teacher uploads a material file to the lesson (multipart). Orphan blob deleted on rejection.</summary>
    [HttpPost("{id:guid}/materials")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(LessonUploadSizeLimit)]
    public async Task<ActionResult<ApiResponse<LessonMaterialDto>>> AddMaterial(
        Guid id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<LessonMaterialDto>.Fail("File is required."));
        if (file.Length > LessonUploadSizeLimit)
            return BadRequest(ApiResponse<LessonMaterialDto>.Fail("File exceeds the 50 MB limit."));

        SavedLessonMaterial saved;
        try
        {
            await using var stream = file.OpenReadStream();
            saved = await lessonFiles.SaveAsync(stream, file.FileName, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<LessonMaterialDto>.Fail(ex.Message));
        }

        var r = await sender.Send(new AddLessonMaterialCommand(
            id, saved.StoredFileName, file.FileName, file.ContentType ?? "application/octet-stream", saved.Size), ct);
        if (!r.Success)
        {
            await lessonFiles.DeleteAsync(saved.StoredFileName, ct);
            return MapFail(r);
        }
        return Ok(ApiResponse<LessonMaterialDto>.Ok(r.Data, r.Message));
    }

    /// <summary>Teacher removes a lesson material (and its blob).</summary>
    [HttpDelete("{id:guid}/materials/{materialId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> RemoveMaterial(Guid id, Guid materialId, CancellationToken ct)
    {
        var r = await sender.Send(new RemoveLessonMaterialCommand(id, materialId), ct);
        if (!r.Success)
            return r.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(ApiResponse<object>.Fail(r.Message ?? "Not found")),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(r.Message ?? "Forbidden")),
                _ => BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed")),
            };
        if (!string.IsNullOrEmpty(r.Data)) await lessonFiles.DeleteAsync(r.Data, ct);
        return Ok(ApiResponse<object>.Ok(new { }, r.Message));
    }

    /// <summary>
    /// Streams a lesson material for IN-PLATFORM viewing. Private bucket — access
    /// + visibility checks run per call in the handler. Served with
    /// Content-Disposition: inline (so PDFs/images/video render in the viewer
    /// instead of forcing a download) and range processing enabled (so the
    /// &lt;video&gt;/&lt;audio&gt; players can seek). A "Download" affordance in the UI
    /// uses the client-side download attribute when the user wants the file.
    /// </summary>
    [HttpGet("materials/{materialId:guid}/download")]
    public async Task<IActionResult> DownloadMaterial(Guid materialId, CancellationToken ct)
    {
        var r = await sender.Send(new GetLessonMaterialForDownloadQuery(materialId), ct);
        if (!r.Success || r.Data is null) return Forbid();
        var stream = await lessonFiles.OpenAsync(r.Data.StoredFileName, ct);
        if (stream is null) return NotFound();
        Response.Headers.ContentDisposition = $"inline; filename=\"{r.Data.OriginalFileName.Replace("\"", "")}\"";
        return File(stream, r.Data.MimeType, enableRangeProcessing: true);
    }

    /// <summary>Teacher sets/clears the lesson video URL.</summary>
    [HttpPost("{id:guid}/video")]
    public async Task<ActionResult<ApiResponse<object>>> SetVideo(
        Guid id, [FromBody] SetVideoRequest body, CancellationToken ct)
    {
        var r = await sender.Send(new SetLessonVideoCommand(id, body.VideoUrl), ct);
        if (r.Success) return Ok(ApiResponse<object>.Ok(new { }, r.Message));
        return r.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(ApiResponse<object>.Fail(r.Message ?? "Not found")),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(r.Message ?? "Forbidden")),
            _ => BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed")),
        };
    }

    /// <summary>Student marks the lesson complete/incomplete (self-scoped).</summary>
    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ApiResponse<object>>> Complete(
        Guid id, [FromBody] CompleteRequest body, CancellationToken ct)
    {
        var r = await sender.Send(new SetLessonProgressCommand(id, body.Completed), ct);
        if (r.Success) return Ok(ApiResponse<object>.Ok(new { }, r.Message));
        return r.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(ApiResponse<object>.Fail(r.Message ?? "Not found")),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(r.Message ?? "Forbidden")),
            _ => BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed")),
        };
    }

    /// <summary>Teacher links one of the class's assignments to this lesson.</summary>
    [HttpPost("{id:guid}/assignments/{assignmentId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> LinkAssignment(Guid id, Guid assignmentId, CancellationToken ct)
    {
        var r = await sender.Send(new LinkLessonAssignmentCommand(id, assignmentId, true), ct);
        if (r.Success) return Ok(ApiResponse<object>.Ok(new { }, r.Message));
        return r.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(ApiResponse<object>.Fail(r.Message ?? "Not found")),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(r.Message ?? "Forbidden")),
            _ => BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed")),
        };
    }

    /// <summary>Teacher unlinks an assignment from this lesson.</summary>
    [HttpDelete("{id:guid}/assignments/{assignmentId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> UnlinkAssignment(Guid id, Guid assignmentId, CancellationToken ct)
    {
        var r = await sender.Send(new LinkLessonAssignmentCommand(id, assignmentId, false), ct);
        if (r.Success) return Ok(ApiResponse<object>.Ok(new { }, r.Message));
        return r.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(ApiResponse<object>.Fail(r.Message ?? "Not found")),
            "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<object>.Fail(r.Message ?? "Forbidden")),
            _ => BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed")),
        };
    }

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(Permissions.Sessions.Delete)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new CancelClassSessionCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}
