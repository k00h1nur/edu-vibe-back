using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Sessions;

public sealed class SessionsHandlers(IApplicationDbContext db, ICurrentUserService currentUser) :
    IRequestHandler<CreateClassSessionCommand, Result<SessionDto>>,
    IRequestHandler<UpdateClassSessionCommand, Result<SessionDto>>,
    IRequestHandler<SetSessionDetailsCommand, Result<SessionDto>>,
    IRequestHandler<CancelClassSessionCommand, Result>,
    IRequestHandler<GetClassSessionsQuery, Result<IReadOnlyCollection<SessionDto>>>,
    IRequestHandler<GetSessionByIdQuery, Result<SessionDto>>,
    IRequestHandler<GetMyScheduleQuery, Result<IReadOnlyCollection<ScheduleEntryDto>>>,
    IRequestHandler<GetUpcomingSessionsQuery, Result<IReadOnlyCollection<ScheduleEntryDto>>>,
    IRequestHandler<GetSessionsForDateQuery, Result<IReadOnlyCollection<SessionDto>>>,
    IRequestHandler<GetScheduleQuery, Result<IReadOnlyCollection<ScheduleEntryDto>>>,
    IRequestHandler<GetClassSchedulePatternQuery, Result<SchedulePatternDto>>,
    IRequestHandler<ApplyClassScheduleCommand, Result<ApplyScheduleResultDto>>
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
        s.SetDetails(request.Topic, request.MeetingUrl, request.Notes);
        await db.ClassSessions.AddAsync(s, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<SessionDto>.Ok(Map(s));
    }

    public async Task<Result<IReadOnlyCollection<SessionDto>>> Handle(GetClassSessionsQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<SessionDto>>.Ok(await db.ClassSessions
            .Where(x => x.ClassId == request.ClassId)
            .Select(x => new SessionDto(x.Id, x.ClassId, x.SessionDate, x.StartsAt, x.EndsAt, x.RoomId, x.Topic, x.MeetingUrl, x.Notes))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<SessionDto>> Handle(GetSessionByIdQuery request,
        CancellationToken cancellationToken)
    {
        var dto = await db.ClassSessions.AsNoTracking()
            .Where(x => x.Id == request.SessionId)
            .Select(x => new SessionDto(x.Id, x.ClassId, x.SessionDate, x.StartsAt, x.EndsAt, x.RoomId, x.Topic, x.MeetingUrl, x.Notes))
            .FirstOrDefaultAsync(cancellationToken);
        return dto is null
            ? Result<SessionDto>.Fail("NOT_FOUND", "Session not found.")
            : Result<SessionDto>.Ok(dto);
    }

    public async Task<Result<IReadOnlyCollection<ScheduleEntryDto>>> Handle(GetMyScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var classIds = await ResolveUserClassIds(request.UserId, cancellationToken);
        return Result<IReadOnlyCollection<ScheduleEntryDto>>.Ok(await db.ClassSessions
            .Where(x => classIds.Contains(x.ClassId))
            .OrderBy(x => x.SessionDate).ThenBy(x => x.StartsAt)
            .Join(db.Classes, s => s.ClassId, c => c.Id, (s, c) => new ScheduleEntryDto(
                s.Id, s.ClassId, c.Title, c.TeacherUserId,
                s.SessionDate, s.StartsAt, s.EndsAt, s.RoomId, s.Topic, s.MeetingUrl))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<IReadOnlyCollection<ScheduleEntryDto>>> Handle(GetUpcomingSessionsQuery request,
        CancellationToken cancellationToken)
    {
        var classIds = await ResolveUserClassIds(request.UserId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var take = Math.Clamp(request.Take, 1, 100);

        var data = await db.ClassSessions
            .Where(x => classIds.Contains(x.ClassId) && x.SessionDate >= today)
            .OrderBy(x => x.SessionDate).ThenBy(x => x.StartsAt)
            .Take(take)
            .Join(db.Classes, s => s.ClassId, c => c.Id, (s, c) => new ScheduleEntryDto(
                s.Id, s.ClassId, c.Title, c.TeacherUserId,
                s.SessionDate, s.StartsAt, s.EndsAt, s.RoomId, s.Topic, s.MeetingUrl))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<ScheduleEntryDto>>.Ok(data);
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
        s.SetDetails(request.Topic, request.MeetingUrl, request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        return Result<SessionDto>.Ok(Map(s));
    }

    /// <summary>
    /// Teacher lesson editor — self-scoped: only the class's own teacher may set
    /// the topic / meeting link / notes. Admins use Create/Update (permission
    /// gated) instead, so this endpoint can stay permission-free.
    /// </summary>
    public async Task<Result<SessionDto>> Handle(SetSessionDetailsCommand request, CancellationToken cancellationToken)
    {
        var s = await db.ClassSessions.FirstOrDefaultAsync(x => x.Id == request.SessionId, cancellationToken);
        if (s is null) return Result<SessionDto>.Fail("NOT_FOUND", "Session not found.");

        var teacherUserId = await db.Classes.AsNoTracking()
            .Where(c => c.Id == s.ClassId)
            .Select(c => c.TeacherUserId)
            .FirstOrDefaultAsync(cancellationToken);
        if (currentUser.UserId is null || teacherUserId != currentUser.UserId)
            return Result<SessionDto>.Fail("FORBIDDEN", "Only the class teacher can edit this lesson.");

        s.SetDetails(request.Topic, request.MeetingUrl, request.Notes);
        await db.SaveChangesAsync(cancellationToken);
        return Result<SessionDto>.Ok(Map(s), "Lesson updated.");
    }

    private static SessionDto Map(ClassSession s)
    {
        return new SessionDto(s.Id, s.ClassId, s.SessionDate, s.StartsAt, s.EndsAt, s.RoomId,
            s.Topic, s.MeetingUrl, s.Notes);
    }

    public async Task<Result<IReadOnlyCollection<SessionDto>>> Handle(GetSessionsForDateQuery request,
        CancellationToken ct)
    {
        var q = db.ClassSessions.AsNoTracking().Where(x => x.SessionDate == request.Date);
        if (request.ClassId is { } classId) q = q.Where(x => x.ClassId == classId);

        var data = await q
            .OrderBy(x => x.StartsAt)
            .Select(x => new SessionDto(x.Id, x.ClassId, x.SessionDate, x.StartsAt, x.EndsAt, x.RoomId, x.Topic, x.MeetingUrl, x.Notes))
            .ToListAsync(ct);
        return Result<IReadOnlyCollection<SessionDto>>.Ok(data);
    }

    public async Task<Result<IReadOnlyCollection<ScheduleEntryDto>>> Handle(GetScheduleQuery request,
        CancellationToken ct)
    {
        if (request.To < request.From)
            return Result<IReadOnlyCollection<ScheduleEntryDto>>.Fail("VALIDATION", "To must be on or after From.");

        var data = await (
            from s in db.ClassSessions.AsNoTracking()
            join c in db.Classes.AsNoTracking() on s.ClassId equals c.Id
            where s.SessionDate >= request.From && s.SessionDate <= request.To
            orderby s.SessionDate, s.StartsAt
            select new ScheduleEntryDto(
                s.Id, s.ClassId, c.Title, c.TeacherUserId,
                s.SessionDate, s.StartsAt, s.EndsAt, s.RoomId, s.Topic, s.MeetingUrl))
            .ToListAsync(ct);
        return Result<IReadOnlyCollection<ScheduleEntryDto>>.Ok(data);
    }

    public async Task<Result<SchedulePatternDto>> Handle(GetClassSchedulePatternQuery request,
        CancellationToken ct)
    {
        var p = await db.ClassSchedulePatterns.AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClassId == request.ClassId, ct);
        return p is null
            ? Result<SchedulePatternDto>.Fail("NOT_FOUND", "No recurring schedule set for this class yet.")
            : Result<SchedulePatternDto>.Ok(MapPattern(p));
    }

    /// <summary>
    /// Upserts the pattern and regenerates the class's sessions.
    ///
    /// What gets touched:
    ///   • Sessions BEFORE today — never (completed history stays intact).
    ///   • Future sessions WITH attendance marks — never (someone already
    ///     recorded reality against them).
    ///   • Every other future session — deleted and replaced by the dates
    ///     the new pattern produces from max(today, StartDate) to EndDate.
    ///
    /// SaveChanges runs once per phase (pattern / deletes / inserts) — the
    /// PG18 + Npgsql 8 row-count bug fires on mixed-statement batches, the
    /// same failure EnrollStudent hit before being split.
    /// </summary>
    public async Task<Result<ApplyScheduleResultDto>> Handle(ApplyClassScheduleCommand request,
        CancellationToken ct)
    {
        var classExists = await db.Classes.AsNoTracking().AnyAsync(c => c.Id == request.ClassId, ct);
        if (!classExists) return Result<ApplyScheduleResultDto>.Fail("NOT_FOUND", "Class not found.");

        if (request.EndDate < request.StartDate)
            return Result<ApplyScheduleResultDto>.Fail("VALIDATION", "End date must be on or after start date.");
        if (request.EndDate.DayNumber - request.StartDate.DayNumber > 366)
            return Result<ApplyScheduleResultDto>.Fail("VALIDATION", "Schedule range is capped at one year.");
        if (request.StartsAt >= request.EndsAt)
            return Result<ApplyScheduleResultDto>.Fail("VALIDATION", "Start time must be before end time.");

        // Phase 1 — upsert the pattern.
        var pattern = await db.ClassSchedulePatterns
            .FirstOrDefaultAsync(x => x.ClassId == request.ClassId, ct);
        try
        {
            if (pattern is null)
            {
                pattern = new ClassSchedulePattern(
                    request.ClassId, request.Type, request.DaysOfWeekMask,
                    request.StartDate, request.EndDate, request.StartsAt, request.EndsAt, request.RoomId);
                await db.ClassSchedulePatterns.AddAsync(pattern, ct);
            }
            else
            {
                pattern.Update(
                    request.Type, request.DaysOfWeekMask,
                    request.StartDate, request.EndDate, request.StartsAt, request.EndsAt, request.RoomId);
            }
        }
        catch (LMS.Domain.Exceptions.DomainException ex)
        {
            return Result<ApplyScheduleResultDto>.Fail("VALIDATION", ex.Message);
        }
        await db.SaveChangesAsync(ct);

        // Phase 2 — drop replaceable future sessions. "Replaceable" = dated
        // today or later AND has no attendance rows.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var futureSessions = await db.ClassSessions
            .Where(s => s.ClassId == request.ClassId && s.SessionDate >= today)
            .ToListAsync(ct);
        var protectedSessionIds = (await db.Attendance.AsNoTracking()
                .Where(a => a.ClassId == request.ClassId)
                .Select(a => a.SessionId)
                .Distinct()
                .ToListAsync(ct))
            .ToHashSet();

        var removable = futureSessions.Where(s => !protectedSessionIds.Contains(s.Id)).ToList();
        var preserved = futureSessions.Count - removable.Count;
        if (removable.Count > 0)
        {
            db.ClassSessions.RemoveRange(removable);
            await db.SaveChangesAsync(ct);
        }

        // Phase 3 — generate. Dates already occupied by a preserved session
        // are skipped so the (ClassId, SessionDate, StartsAt) unique index
        // can't collide with a session we deliberately kept.
        var occupiedDates = futureSessions
            .Where(s => protectedSessionIds.Contains(s.Id))
            .Select(s => s.SessionDate)
            .ToHashSet();

        var generateFrom = request.StartDate > today ? request.StartDate : today;
        var generated = 0;
        for (var d = generateFrom; d <= request.EndDate; d = d.AddDays(1))
        {
            if (!pattern.Matches(d) || occupiedDates.Contains(d)) continue;
            await db.ClassSessions.AddAsync(
                new ClassSession(request.ClassId, d, request.StartsAt, request.EndsAt, request.RoomId), ct);
            generated++;
        }
        if (generated > 0) await db.SaveChangesAsync(ct);

        return Result<ApplyScheduleResultDto>.Ok(
            new ApplyScheduleResultDto(MapPattern(pattern), generated, removable.Count, preserved),
            $"Generated {generated} lesson(s).");
    }

    private static SchedulePatternDto MapPattern(ClassSchedulePattern p) => new(
        p.ClassId, p.Type, p.DaysOfWeekMask,
        p.StartDate, p.EndDate, p.StartsAt, p.EndsAt, p.RoomId, p.UpdatedAt);
}