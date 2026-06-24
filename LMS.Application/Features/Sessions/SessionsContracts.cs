using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Sessions;

public sealed record SessionDto(
    Guid Id,
    Guid ClassId,
    DateOnly SessionDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? RoomId,
    string? Topic = null,
    string? MeetingUrl = null,
    string? Notes = null);

public sealed record SessionsPingCommand : IRequest<Result<string>>;

public sealed class SessionsPingCommandHandler : IRequestHandler<SessionsPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(SessionsPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Sessions module ready"));
    }
}

public sealed record CreateClassSessionCommand(
    Guid ClassId,
    DateOnly SessionDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? RoomId,
    string? Topic = null,
    string? MeetingUrl = null,
    string? Notes = null) : IRequest<Result<SessionDto>>;

public sealed record UpdateClassSessionCommand(
    Guid SessionId,
    DateOnly SessionDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? RoomId,
    string? Topic = null,
    string? MeetingUrl = null,
    string? Notes = null) : IRequest<Result<SessionDto>>;

/// <summary>
/// Teacher-facing lesson editor: set the topic, online meeting link (Zoom /
/// Google Meet) and notes on a session WITHOUT touching its date/time. The
/// handler self-scopes to the class's own teacher, so it needs no
/// Sessions.Update permission (which is admin-only).
/// </summary>
public sealed record SetSessionDetailsCommand(
    Guid SessionId,
    string? Topic,
    string? MeetingUrl,
    string? Notes) : IRequest<Result<SessionDto>>;

public sealed record CancelClassSessionCommand(Guid SessionId) : IRequest<Result>;

public sealed record GetClassSessionsQuery(Guid ClassId) : IRequest<Result<IReadOnlyCollection<SessionDto>>>;

/// <summary>
/// Single-session lookup by id — powers the per-session marking pages in the
/// frontend. Without it, callers walk every class's session list to find one.
/// </summary>
public sealed record GetSessionByIdQuery(Guid SessionId) : IRequest<Result<SessionDto>>;

/// <summary>
/// Returns <see cref="ScheduleEntryDto"/> (not the lean SessionDto) so the
/// caller gets the class title in the same round-trip — students can't read
/// /api/Classes, so the join is the only way their timetable shows names.
/// </summary>
public sealed record GetMyScheduleQuery(Guid UserId) : IRequest<Result<IReadOnlyCollection<ScheduleEntryDto>>>;

/// <summary>
/// Upcoming sessions for the given user. Resolves both teaching schedule (for staff/teachers)
/// and enrolled classes (for students), starting from today. Same joined shape
/// as <see cref="GetMyScheduleQuery"/>.
/// </summary>
public sealed record GetUpcomingSessionsQuery(Guid UserId, int Take = 20)
    : IRequest<Result<IReadOnlyCollection<ScheduleEntryDto>>>;

/// <summary>
/// Admin-scope query — every session on a given date across all classes.
/// Powers the admin "Today's lessons" attendance entry point. When
/// <see cref="ClassId"/> is set the query is restricted to that class.
/// </summary>
public sealed record GetSessionsForDateQuery(DateOnly Date, Guid? ClassId = null)
    : IRequest<Result<IReadOnlyCollection<SessionDto>>>;

/// <summary>
/// Same row as <see cref="SessionDto"/> plus the joined class title and
/// teacher id. Cheap enough to compute in a single query and saves the
/// admin schedule page from N extra round-trips.
/// </summary>
public sealed record ScheduleEntryDto(
    Guid Id,
    Guid ClassId,
    string ClassTitle,
    Guid? TeacherUserId,
    DateOnly SessionDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? RoomId,
    string? Topic = null,
    string? MeetingUrl = null);

/// <summary>
/// Returns every session inside [<paramref name="From"/>, <paramref name="To"/>]
/// joined with the class title — what the admin schedule view paints into the
/// week grid. Dates are inclusive on both ends.
/// </summary>
public sealed record GetScheduleQuery(DateOnly From, DateOnly To)
    : IRequest<Result<IReadOnlyCollection<ScheduleEntryDto>>>;

/// <summary>
/// Admin schedule report row (F2) — one per session, enriched with the teacher's
/// full name, the curriculum lesson topic, and present-vs-enrolled counts. Heavier
/// than <see cref="ScheduleEntryDto"/> (per-session aggregation), so it lives in its
/// own query rather than bloating the lightweight schedule grid.
/// </summary>
public sealed record AdminScheduleEntryDto(
    Guid Id,
    Guid ClassId,
    string ClassName,
    Guid? TeacherUserId,
    string? TeacherFullName,
    DateOnly SessionDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    string? Topic,
    int PresentCount,
    int EnrolledCount);

/// <summary>
/// Admin schedule across [<paramref name="From"/>, <paramref name="To"/>], optionally
/// narrowed to one teacher and/or one class. Present/enrolled counts are aggregated in
/// grouped queries (never per session). Dates inclusive.
/// </summary>
public sealed record GetAdminScheduleQuery(
    DateOnly From,
    DateOnly To,
    Guid? TeacherId = null,
    Guid? ClassId = null) : IRequest<Result<IReadOnlyCollection<AdminScheduleEntryDto>>>;

// ---- Recurring schedule patterns -----------------------------------------

public sealed record SchedulePatternDto(
    Guid ClassId,
    SchedulePatternType Type,
    int DaysOfWeekMask,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? RoomId,
    DateTime UpdatedAt);

/// <summary>The class's recurring pattern, or NOT_FOUND when none was set yet.</summary>
public sealed record GetClassSchedulePatternQuery(Guid ClassId) : IRequest<Result<SchedulePatternDto>>;

/// <summary>
/// Upserts the class's recurring pattern and regenerates its sessions:
/// past sessions and any session that already has attendance marks stay
/// untouched; every other future session is replaced by the dates the new
/// pattern produces. Replaces lesson-by-lesson manual creation.
/// </summary>
/// <summary>One lesson time-slot within a day (F3 multi-lesson-per-day support).</summary>
public sealed record ScheduleSlot(TimeOnly StartsAt, TimeOnly EndsAt);

public sealed record ApplyClassScheduleCommand(
    Guid ClassId,
    SchedulePatternType Type,
    int DaysOfWeekMask,
    DateOnly StartDate,
    DateOnly EndDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? RoomId,
    /// <summary>
    /// Optional 2–3 lessons/day slots (F3). When set, one session per slot is
    /// generated on each matched day; when null/empty, the single StartsAt/EndsAt
    /// is used — so the existing schedule-pattern caller is unchanged. Not persisted
    /// to the recurring pattern (the pattern keeps its single StartsAt/EndsAt).
    /// </summary>
    IReadOnlyList<ScheduleSlot>? Slots = null) : IRequest<Result<ApplyScheduleResultDto>>;

/// <summary>What the apply did — surfaced as the admin-facing toast.</summary>
public sealed record ApplyScheduleResultDto(
    SchedulePatternDto Pattern,
    int GeneratedCount,
    int RemovedCount,
    int PreservedCount);