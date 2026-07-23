using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
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
    IRequestHandler<GetAdminScheduleQuery, Result<IReadOnlyCollection<AdminScheduleEntryDto>>>,
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
        var teachingClassIds = await TeachingClassIds(request.UserId, cancellationToken);
        var now = DateTime.UtcNow;
        return Result<IReadOnlyCollection<ScheduleEntryDto>>.Ok(await db.ClassSessions
            // VISIBILITY: as a student you only see lessons whose content is
            // currently visible; as the teacher of a class you see all of yours.
            .Where(x => classIds.Contains(x.ClassId)
                && (teachingClassIds.Contains(x.ClassId)
                    || (x.IsPublished
                        && (x.VisibleFrom == null || x.VisibleFrom <= now)
                        && (x.VisibleUntil == null || now <= x.VisibleUntil))))
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
        var teachingClassIds = await TeachingClassIds(request.UserId, cancellationToken);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTime.UtcNow;
        var take = Math.Clamp(request.Take, 1, 100);

        var data = await db.ClassSessions
            // Same visibility gate as GetMySchedule (students see visible only).
            .Where(x => classIds.Contains(x.ClassId) && x.SessionDate >= today
                && (teachingClassIds.Contains(x.ClassId)
                    || (x.IsPublished
                        && (x.VisibleFrom == null || x.VisibleFrom <= now)
                        && (x.VisibleUntil == null || now <= x.VisibleUntil))))
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

        var touched = enrolledClassIds.Union(teachingClassIds);

        // Archived (Cancelled) classes never surface on a schedule, even if an
        // enrolment or teacher link still points at them (cancel is a soft-delete).
        return await db.Classes
            .Where(c => touched.Contains(c.Id) && c.Status != ClassStatus.Cancelled)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Class ids the user teaches — used to exempt a teacher's own lessons from the student visibility gate.</summary>
    private async Task<List<Guid>> TeachingClassIds(Guid userId, CancellationToken cancellationToken) =>
        await db.Classes.AsNoTracking()
            .Where(c => c.TeacherUserId == userId)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

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
                  && c.Status != ClassStatus.Cancelled
            orderby s.SessionDate, s.StartsAt
            select new ScheduleEntryDto(
                s.Id, s.ClassId, c.Title, c.TeacherUserId,
                s.SessionDate, s.StartsAt, s.EndsAt, s.RoomId, s.Topic, s.MeetingUrl))
            .ToListAsync(ct);
        return Result<IReadOnlyCollection<ScheduleEntryDto>>.Ok(data);
    }

    public async Task<Result<IReadOnlyCollection<AdminScheduleEntryDto>>> Handle(GetAdminScheduleQuery request,
        CancellationToken ct)
    {
        if (request.To < request.From)
            return Result<IReadOnlyCollection<AdminScheduleEntryDto>>.Fail("VALIDATION", "To must be on or after From.");

        // 1) Base rows: session + class + (optional) teacher name + curriculum topic.
        //    StaffProfile is LEFT-joined on UserId == Class.TeacherUserId (not a modelled
        //    relationship); the curriculum lesson title is the authoritative topic when the
        //    session is curriculum-linked, otherwise the ad-hoc Topic.
        var baseRows = await (
            from s in db.ClassSessions.AsNoTracking()
            join c in db.Classes.AsNoTracking() on s.ClassId equals c.Id
            join sp in db.StaffProfiles.AsNoTracking() on c.TeacherUserId equals sp.UserId into sps
            from sp in sps.DefaultIfEmpty()
            where s.SessionDate >= request.From && s.SessionDate <= request.To
                  && !s.IsBackfilled
                  && c.Status != ClassStatus.Cancelled
                  && (request.TeacherId == null || c.TeacherUserId == request.TeacherId)
                  && (request.ClassId == null || s.ClassId == request.ClassId)
            orderby s.SessionDate, s.StartsAt
            select new
            {
                s.Id,
                s.ClassId,
                ClassName = c.Title,
                c.TeacherUserId,
                TeacherFirst = sp != null ? sp.FirstName : null,
                TeacherLast = sp != null ? sp.LastName : null,
                s.SessionDate,
                s.StartsAt,
                s.EndsAt,
                Topic = s.CurriculumLessonId != null ? s.CurriculumLesson!.Title : s.Topic,
            }).ToListAsync(ct);

        if (baseRows.Count == 0)
            return Result<IReadOnlyCollection<AdminScheduleEntryDto>>.Ok(Array.Empty<AdminScheduleEntryDto>());

        var sessionIds = baseRows.Select(r => r.Id).ToList();
        var classIds = baseRows.Select(r => r.ClassId).Distinct().ToList();

        // 2) Present counts — ONE grouped query over Attendance, keyed by session (no N+1).
        var presentBySession = (await db.Attendance.AsNoTracking()
                .Where(a => sessionIds.Contains(a.SessionId) && a.Status == AttendanceStatus.Present)
                .GroupBy(a => a.SessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => x.SessionId, x => x.Count);

        // 3) Active-enrolment counts — ONE grouped query over Enrollments, keyed by class.
        var enrolledByClass = (await db.Enrollments.AsNoTracking()
                .Where(e => classIds.Contains(e.ClassId) && e.Status == EnrollmentStatus.Active)
                .GroupBy(e => e.ClassId)
                .Select(g => new { ClassId = g.Key, Count = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => x.ClassId, x => x.Count);

        var rows = baseRows.Select(r =>
        {
            var name = $"{r.TeacherFirst ?? ""} {r.TeacherLast ?? ""}".Trim();
            return new AdminScheduleEntryDto(
                r.Id, r.ClassId, r.ClassName, r.TeacherUserId,
                string.IsNullOrEmpty(name) ? null : name,
                r.SessionDate, r.StartsAt, r.EndsAt, r.Topic,
                presentBySession.TryGetValue(r.Id, out var pc) ? pc : 0,
                enrolledByClass.TryGetValue(r.ClassId, out var ec) ? ec : 0);
        }).ToList();

        return Result<IReadOnlyCollection<AdminScheduleEntryDto>>.Ok(rows);
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

        // Effective per-day slots (F3): caller-supplied slots if any (deduped by
        // start time so the (ClassId, SessionDate, StartsAt) unique index can't
        // collide), else the single primary slot — i.e. the original behaviour.
        var slots = request.Slots is { Count: > 0 }
            ? request.Slots.GroupBy(s => s.StartsAt).Select(g => g.First()).OrderBy(s => s.StartsAt).ToList()
            : new List<ScheduleSlot> { new(request.StartsAt, request.EndsAt) };
        if (slots.Any(s => s.StartsAt >= s.EndsAt))
            return Result<ApplyScheduleResultDto>.Fail("VALIDATION", "Each slot's start time must be before its end time.");

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

        // Phase 2 — drop replaceable future sessions. "Replaceable" = dated today or
        // later AND carrying no real dependent data — NO attendance marks AND NO
        // materialised homework. Preserving homework'd sessions (stable Id) means a
        // generate/reschedule re-run can never orphan or duplicate auto-materialised
        // tasks or student submissions; moving such a lesson requires clearing its
        // homework first (a deliberate act, not an accidental side effect).
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var futureSessions = await db.ClassSessions
            .Where(s => s.ClassId == request.ClassId && s.SessionDate >= today)
            .ToListAsync(ct);

        var attendanceSessionIds = await db.Attendance.AsNoTracking()
            .Where(a => a.ClassId == request.ClassId)
            .Select(a => a.SessionId).Distinct().ToListAsync(ct);
        // "Has homework" = an assignment linked to the session with at least one task.
        var homeworkSessionIds = await db.Assignments.AsNoTracking()
            .Where(a => a.ClassId == request.ClassId && a.ClassSessionId != null
                        && db.LearningTasks.Any(t => t.AssignmentId == a.Id))
            .Select(a => a.ClassSessionId!.Value).Distinct().ToListAsync(ct);
        var protectedSessionIds = attendanceSessionIds.Concat(homeworkSessionIds).ToHashSet();

        // Pure core decides what's removable + which dates stay occupied.
        var plan = SchedulePreserve.Partition(
            futureSessions.Select(s => new ScheduleSessionRef(s.Id, s.SessionDate)).ToList(),
            protectedSessionIds);
        var removableIds = plan.RemovableIds.ToHashSet();
        var removable = futureSessions.Where(s => removableIds.Contains(s.Id)).ToList();
        var preserved = futureSessions.Count - removable.Count;
        if (removable.Count > 0)
        {
            db.ClassSessions.RemoveRange(removable);
            await db.SaveChangesAsync(ct);
        }

        // Phase 3 — generate. Dates already occupied by a preserved session
        // are skipped so the (ClassId, SessionDate, StartsAt) unique index
        // can't collide with a session we deliberately kept.
        var occupiedDates = plan.OccupiedDates;

        var generateFrom = request.StartDate > today ? request.StartDate : today;
        var generated = 0;
        for (var d = generateFrom; d <= request.EndDate; d = d.AddDays(1))
        {
            // A date kept for a preserved (attendance-bearing) session is skipped
            // wholesale — we never partially repopulate it — which also keeps the
            // unique index safe.
            if (!pattern.Matches(d) || occupiedDates.Contains(d)) continue;
            foreach (var slot in slots)
            {
                await db.ClassSessions.AddAsync(
                    new ClassSession(request.ClassId, d, slot.StartsAt, slot.EndsAt, request.RoomId), ct);
                generated++;
            }
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