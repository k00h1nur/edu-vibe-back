using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Security;
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
public sealed class ClassSessionsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
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
