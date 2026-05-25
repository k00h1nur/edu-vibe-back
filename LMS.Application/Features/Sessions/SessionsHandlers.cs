using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Sessions;

public sealed class SessionsHandlers(IApplicationDbContext db) :
    IRequestHandler<CreateClassSessionCommand, Result<SessionDto>>,
    IRequestHandler<UpdateClassSessionCommand, Result<SessionDto>>,
    IRequestHandler<CancelClassSessionCommand, Result>,
    IRequestHandler<GetClassSessionsQuery, Result<IReadOnlyCollection<SessionDto>>>,
    IRequestHandler<GetMyScheduleQuery, Result<IReadOnlyCollection<SessionDto>>>,
    IRequestHandler<GetUpcomingSessionsQuery, Result<IReadOnlyCollection<SessionDto>>>
{
    public async Task<Result> Handle(CancelClassSessionCommand request, CancellationToken cancellationToken)
    {
        var s = await db.ClassSessions.FirstOrDefaultAsync(x => x.Id == request.SessionId, cancellationToken);
        if (s is null) return Result.Fail("NOT_FOUND", "Session not found.");
        db.ClassSessions.Remove(s);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Deleted");
    }

    public async Task<Result<SessionDto>> Handle(CreateClassSessionCommand request, CancellationToken cancellationToken)
    {
        var s = new ClassSession(request.ClassId, request.SessionDate, request.StartsAt, request.EndsAt,
            request.RoomId);
        await db.ClassSessions.AddAsync(s, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<SessionDto>.Ok(Map(s));
    }

    public async Task<Result<IReadOnlyCollection<SessionDto>>> Handle(GetClassSessionsQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<SessionDto>>.Ok(await db.ClassSessions
            .Where(x => x.ClassId == request.ClassId)
            .Select(x => new SessionDto(x.Id, x.ClassId, x.SessionDate, x.StartsAt, x.EndsAt, x.RoomId))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<IReadOnlyCollection<SessionDto>>> Handle(GetMyScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var classIds = await ResolveUserClassIds(request.UserId, cancellationToken);
        return Result<IReadOnlyCollection<SessionDto>>.Ok(await db.ClassSessions
            .Where(x => classIds.Contains(x.ClassId))
            .OrderBy(x => x.SessionDate).ThenBy(x => x.StartsAt)
            .Select(x => new SessionDto(x.Id, x.ClassId, x.SessionDate, x.StartsAt, x.EndsAt, x.RoomId))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<IReadOnlyCollection<SessionDto>>> Handle(GetUpcomingSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var classIds = await ResolveUserClassIds(request.UserId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var take = Math.Clamp(request.Take, 1, 100);

        var data = await db.ClassSessions
            .Where(x => classIds.Contains(x.ClassId) && x.SessionDate >= today)
            .OrderBy(x => x.SessionDate).ThenBy(x => x.StartsAt)
            .Take(take)
            .Select(x => new SessionDto(x.Id, x.ClassId, x.SessionDate, x.StartsAt, x.EndsAt, x.RoomId))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<SessionDto>>.Ok(data);
    }

    /// <summary>
    /// Returns the class ids the user touches — both as a student (via enrollment)
    /// and as a teacher (via Class.TeacherUserId). One round-trip via UNION; the
    /// student/teacher branches join against StudentProfiles and Classes in SQL.
    /// </summary>
    private async Task<List<Guid>> ResolveUserClassIds(Guid userId, CancellationToken cancellationToken)
    {
        var enrolledClassIds = db.Enrollments
            .Where(e => db.StudentProfiles.Any(sp => sp.UserId == userId && sp.Id == e.StudentProfileId))
            .Select(e => e.ClassId);

        var teachingClassIds = db.Classes
            .Where(c => c.TeacherUserId == userId)
            .Select(c => c.Id);

        return await enrolledClassIds
            .Union(teachingClassIds)
            .ToListAsync(cancellationToken);
    }

    public async Task<Result<SessionDto>> Handle(UpdateClassSessionCommand request, CancellationToken cancellationToken)
    {
        var s = await db.ClassSessions.FirstOrDefaultAsync(x => x.Id == request.SessionId, cancellationToken);
        if (s is null) return Result<SessionDto>.Fail("NOT_FOUND", "Session not found.");
        s.Reschedule(request.SessionDate, request.StartsAt, request.EndsAt, request.RoomId);
        await db.SaveChangesAsync(cancellationToken);
        return Result<SessionDto>.Ok(Map(s));
    }

    private static SessionDto Map(ClassSession s)
    {
        return new SessionDto(s.Id, s.ClassId, s.SessionDate, s.StartsAt, s.EndsAt, s.RoomId);
    }
}