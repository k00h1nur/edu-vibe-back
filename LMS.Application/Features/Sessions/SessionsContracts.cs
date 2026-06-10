using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Sessions;

public sealed record SessionDto(
    Guid Id,
    Guid ClassId,
    DateOnly SessionDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? RoomId);

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
    Guid? RoomId) : IRequest<Result<SessionDto>>;

public sealed record UpdateClassSessionCommand(
    Guid SessionId,
    DateOnly SessionDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? RoomId) : IRequest<Result<SessionDto>>;

public sealed record CancelClassSessionCommand(Guid SessionId) : IRequest<Result>;

public sealed record GetClassSessionsQuery(Guid ClassId) : IRequest<Result<IReadOnlyCollection<SessionDto>>>;

/// <summary>
/// Single-session lookup by id — powers the per-session marking pages in the
/// frontend. Without it, callers walk every class's session list to find one.
/// </summary>
public sealed record GetSessionByIdQuery(Guid SessionId) : IRequest<Result<SessionDto>>;

public sealed record GetMyScheduleQuery(Guid UserId) : IRequest<Result<IReadOnlyCollection<SessionDto>>>;

/// <summary>
/// Upcoming sessions for the given user. Resolves both teaching schedule (for staff/teachers)
/// and enrolled classes (for students), starting from today.
/// </summary>
public sealed record GetUpcomingSessionsQuery(Guid UserId, int Take = 20)
    : IRequest<Result<IReadOnlyCollection<SessionDto>>>;

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
    Guid? RoomId);

/// <summary>
/// Returns every session inside [<paramref name="From"/>, <paramref name="To"/>]
/// joined with the class title — what the admin schedule view paints into the
/// week grid. Dates are inclusive on both ends.
/// </summary>
public sealed record GetScheduleQuery(DateOnly From, DateOnly To)
    : IRequest<Result<IReadOnlyCollection<ScheduleEntryDto>>>;