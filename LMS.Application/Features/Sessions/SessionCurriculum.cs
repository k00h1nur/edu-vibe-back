using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Sessions;

// ---- Schedule↔Curriculum DTOs --------------------------------------------

/// <summary>A rich schedule card — the session joined with its curriculum unit/lesson + counts.</summary>
public sealed record ScheduleCardDto(
    Guid Id, Guid ClassId, string ClassTitle, DateOnly SessionDate, TimeOnly StartsAt, TimeOnly EndsAt,
    ClassSessionStatus Status, DateTime? CompletedAt, string? Topic, string? MeetingUrl,
    Guid? CurriculumLessonId, int? UnitOrder, string? UnitTitle, int? LessonOrder, string? LessonTitle,
    CurriculumLessonType? LessonType, int MaterialCount, int HomeworkCount);

public sealed record GetClassScheduleCardsQuery(Guid ClassId) : IRequest<Result<IReadOnlyCollection<ScheduleCardDto>>>;

/// <summary>Plan a session against the curriculum: pick the lesson, set topic + teacher notes.</summary>
public sealed record SetSessionCurriculumCommand(Guid SessionId, Guid? CurriculumLessonId, string? Topic, string? Notes)
    : IRequest<Result<ScheduleCardDto>>;

/// <summary>Move a session through its teaching lifecycle (Planned/InProgress/Completed/Cancelled).</summary>
public sealed record SetSessionStatusCommand(Guid SessionId, ClassSessionStatus Status)
    : IRequest<Result<ScheduleCardDto>>;

public sealed record LessonPlanMaterialDto(Guid Id, string FileName, string MimeType, long FileSize);

public sealed record LessonPlanAttendanceDto(
    int Present, int Absent, int Late, int Excused, int Total, IReadOnlyList<string> MissedStudents);

/// <summary>The full teaching plan for one session — class + curriculum + content + attendance stats.</summary>
public sealed record SessionLessonPlanDto(
    Guid SessionId, Guid ClassId, string ClassTitle, string? TeacherName,
    DateOnly SessionDate, TimeOnly StartsAt, TimeOnly EndsAt, ClassSessionStatus Status, DateTime? CompletedAt,
    string? Topic, string? Notes, string? MeetingUrl,
    Guid? CurriculumLessonId, int? UnitOrder, string? UnitTitle, int? LessonOrder, string? LessonTitle,
    CurriculumLessonType? LessonType, string? Objectives, int? DurationMinutes, int XpReward, bool IsAssessment,
    string? MaterialsNote, string? HomeworkNote,
    IReadOnlyList<LessonPlanMaterialDto> Materials, LessonPlanAttendanceDto Attendance);

public sealed record GetSessionLessonPlanQuery(Guid SessionId) : IRequest<Result<SessionLessonPlanDto>>;

// ---- Handler --------------------------------------------------------------

/// <summary>
/// Schedule↔Curriculum integration. Lets the class teacher (or an admin) plan
/// each session against a curriculum lesson, drive its teaching status, read a
/// rich schedule + a full lesson plan. Completing a session is the signal the
/// student roadmap reads to mark a lesson done — the schedule is the source of
/// truth for what was taught.
/// </summary>
public sealed class SessionCurriculumHandler(IApplicationDbContext db, ICurrentUserService currentUser) :
    IRequestHandler<GetClassScheduleCardsQuery, Result<IReadOnlyCollection<ScheduleCardDto>>>,
    IRequestHandler<SetSessionCurriculumCommand, Result<ScheduleCardDto>>,
    IRequestHandler<SetSessionStatusCommand, Result<ScheduleCardDto>>,
    IRequestHandler<GetSessionLessonPlanQuery, Result<SessionLessonPlanDto>>
{
    private bool IsAdmin =>
        currentUser.IsInRole(RoleCodes.Admin) || currentUser.IsInRole(RoleCodes.SuperAdmin)
        || currentUser.IsInRole(RoleCodes.OfficeAdmin);

    public async Task<Result<IReadOnlyCollection<ScheduleCardDto>>> Handle(
        GetClassScheduleCardsQuery request, CancellationToken ct)
    {
        var title = await db.Classes.AsNoTracking().Where(c => c.Id == request.ClassId)
            .Select(c => c.Title).FirstOrDefaultAsync(ct);
        if (title is null) return Result<IReadOnlyCollection<ScheduleCardDto>>.Fail("NOT_FOUND", "Class not found.");

        var cards = await BuildCardsAsync(request.ClassId, title, ct);
        return Result<IReadOnlyCollection<ScheduleCardDto>>.Ok(cards);
    }

    public async Task<Result<ScheduleCardDto>> Handle(SetSessionCurriculumCommand request, CancellationToken ct)
    {
        var s = await db.ClassSessions.FirstOrDefaultAsync(x => x.Id == request.SessionId, ct);
        if (s is null) return Result<ScheduleCardDto>.Fail("NOT_FOUND", "Session not found.");
        if (!await CanManageAsync(s.ClassId, ct))
            return Result<ScheduleCardDto>.Fail("FORBIDDEN", "Only the class teacher or an admin can plan this lesson.");

        // Integrity: a chosen lesson must belong to this class's own course.
        if (request.CurriculumLessonId is { } lessonId)
        {
            var templateId = await db.Classes.AsNoTracking().Where(c => c.Id == s.ClassId)
                .Select(c => c.CurriculumTemplateId).FirstOrDefaultAsync(ct);
            var inCourse = templateId is { } tid && await (
                from l in db.CurriculumLessons.AsNoTracking()
                join u in db.CurriculumUnits.AsNoTracking() on l.UnitId equals u.Id
                join m in db.CurriculumModules.AsNoTracking() on u.ModuleId equals m.Id
                where l.Id == lessonId && m.TemplateId == tid
                select l.Id).AnyAsync(ct);
            if (!inCourse)
                return Result<ScheduleCardDto>.Fail("VALIDATION", "That lesson isn't part of this class's course.");
        }

        s.PlanCurriculum(request.CurriculumLessonId, request.Topic, request.Notes);
        await db.SaveChangesAsync(ct);
        return Result<ScheduleCardDto>.Ok(await BuildOneCardAsync(s.Id, ct), "Lesson planned.");
    }

    public async Task<Result<ScheduleCardDto>> Handle(SetSessionStatusCommand request, CancellationToken ct)
    {
        var s = await db.ClassSessions.FirstOrDefaultAsync(x => x.Id == request.SessionId, ct);
        if (s is null) return Result<ScheduleCardDto>.Fail("NOT_FOUND", "Session not found.");
        if (!await CanManageAsync(s.ClassId, ct))
            return Result<ScheduleCardDto>.Fail("FORBIDDEN", "Only the class teacher or an admin can update this lesson.");

        s.SetStatus(request.Status, DateTime.UtcNow);
        await db.SaveChangesAsync(ct);
        return Result<ScheduleCardDto>.Ok(await BuildOneCardAsync(s.Id, ct),
            request.Status == ClassSessionStatus.Completed ? "Lesson completed." : "Lesson updated.");
    }

    public async Task<Result<SessionLessonPlanDto>> Handle(GetSessionLessonPlanQuery request, CancellationToken ct)
    {
        var s = await db.ClassSessions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.SessionId, ct);
        if (s is null) return Result<SessionLessonPlanDto>.Fail("NOT_FOUND", "Session not found.");
        if (!await CanManageAsync(s.ClassId, ct))
            return Result<SessionLessonPlanDto>.Fail("FORBIDDEN", "Only the class teacher or an admin can view this plan.");

        var cls = await db.Classes.AsNoTracking().Where(c => c.Id == s.ClassId)
            .Select(c => new { c.Title, c.TeacherUserId }).FirstAsync(ct);
        string? teacherName = null;
        if (cls.TeacherUserId is { } tuid)
            teacherName = await db.StaffProfiles.AsNoTracking().Where(p => p.UserId == tuid)
                .Select(p => ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim()).FirstOrDefaultAsync(ct);

        // Curriculum lesson + unit (if planned).
        var lp = s.CurriculumLessonId is { } lid
            ? await (from l in db.CurriculumLessons.AsNoTracking()
                     join u in db.CurriculumUnits.AsNoTracking() on l.UnitId equals u.Id
                     where l.Id == lid
                     select new
                     {
                         u.Order, u.Title, LessonOrder = l.Order, LessonTitle = l.Title,
                         l.LessonType, l.Objectives, l.DurationMinutes, l.XpReward, l.IsAssessment,
                         l.MaterialsPlaceholder, l.HomeworkPlaceholder,
                     }).FirstOrDefaultAsync(ct)
            : null;

        var materials = await db.LessonMaterials.AsNoTracking()
            .Where(m => m.ClassSessionId == s.Id)
            .Select(m => new LessonPlanMaterialDto(m.Id, m.OriginalFileName, m.MimeType, m.FileSize))
            .ToListAsync(ct);

        // Attendance summary.
        var att = await db.Attendance.AsNoTracking().Where(a => a.SessionId == s.Id)
            .Select(a => new { a.Status, a.StudentProfileId }).ToListAsync(ct);
        var absentIds = att.Where(a => a.Status == AttendanceStatus.Absent).Select(a => a.StudentProfileId).ToList();
        var missed = absentIds.Count == 0
            ? new List<string>()
            : await db.StudentProfiles.AsNoTracking().Where(p => absentIds.Contains(p.Id))
                .Select(p => ((p.FirstName ?? "") + " " + (p.LastName ?? "")).Trim())
                .ToListAsync(ct);
        var attendance = new LessonPlanAttendanceDto(
            att.Count(a => a.Status == AttendanceStatus.Present),
            att.Count(a => a.Status == AttendanceStatus.Absent),
            att.Count(a => a.Status == AttendanceStatus.Late),
            att.Count(a => a.Status == AttendanceStatus.Excused),
            att.Count, missed.Where(n => !string.IsNullOrWhiteSpace(n)).ToList());

        var dto = new SessionLessonPlanDto(
            s.Id, s.ClassId, cls.Title, string.IsNullOrWhiteSpace(teacherName) ? null : teacherName,
            s.SessionDate, s.StartsAt, s.EndsAt, s.Status, s.CompletedAt,
            s.Topic, s.Notes, s.MeetingUrl,
            s.CurriculumLessonId, lp?.Order, lp?.Title, lp?.LessonOrder, lp?.LessonTitle,
            lp?.LessonType, lp?.Objectives, lp?.DurationMinutes, lp?.XpReward ?? 0, lp?.IsAssessment ?? false,
            lp?.MaterialsPlaceholder, lp?.HomeworkPlaceholder, materials, attendance);
        return Result<SessionLessonPlanDto>.Ok(dto);
    }

    // ---- helpers ----------------------------------------------------------

    private async Task<bool> CanManageAsync(Guid classId, CancellationToken ct)
    {
        if (IsAdmin) return true;
        var teacher = await db.Classes.AsNoTracking().Where(c => c.Id == classId)
            .Select(c => c.TeacherUserId).FirstOrDefaultAsync(ct);
        return currentUser.UserId is not null && teacher == currentUser.UserId;
    }

    private async Task<ScheduleCardDto> BuildOneCardAsync(Guid sessionId, CancellationToken ct)
    {
        var classId = await db.ClassSessions.AsNoTracking().Where(s => s.Id == sessionId)
            .Select(s => s.ClassId).FirstAsync(ct);
        var title = await db.Classes.AsNoTracking().Where(c => c.Id == classId).Select(c => c.Title).FirstAsync(ct);
        var cards = await BuildCardsAsync(classId, title, ct, sessionId);
        return cards.First();
    }

    private async Task<List<ScheduleCardDto>> BuildCardsAsync(
        Guid classId, string classTitle, CancellationToken ct, Guid? onlySessionId = null)
    {
        var q = db.ClassSessions.AsNoTracking().Where(s => s.ClassId == classId);
        if (onlySessionId is { } sid) q = q.Where(s => s.Id == sid);
        var sessions = await q.OrderBy(s => s.SessionDate).ThenBy(s => s.StartsAt)
            .Select(s => new
            {
                s.Id, s.SessionDate, s.StartsAt, s.EndsAt, s.Status, s.CompletedAt,
                s.Topic, s.MeetingUrl, s.CurriculumLessonId,
            }).ToListAsync(ct);

        var lessonIds = sessions.Where(s => s.CurriculumLessonId != null)
            .Select(s => s.CurriculumLessonId!.Value).Distinct().ToList();
        var lessonInfo = lessonIds.Count == 0
            ? new Dictionary<Guid, dynamic>()
            : (await (from l in db.CurriculumLessons.AsNoTracking()
                      join u in db.CurriculumUnits.AsNoTracking() on l.UnitId equals u.Id
                      where lessonIds.Contains(l.Id)
                      select new
                      {
                          l.Id, UnitOrder = u.Order, UnitTitle = u.Title,
                          LessonOrder = l.Order, LessonTitle = l.Title, l.LessonType, l.HomeworkPlaceholder,
                      }).ToListAsync(ct)).ToDictionary(x => x.Id, x => (dynamic)x);

        var sessionIds = sessions.Select(s => s.Id).ToList();
        var matCounts = (await db.LessonMaterials.AsNoTracking()
                .Where(m => sessionIds.Contains(m.ClassSessionId))
                .GroupBy(m => m.ClassSessionId)
                .Select(g => new { SessionId = g.Key, Count = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => x.SessionId, x => x.Count);

        return sessions.Select(s =>
        {
            lessonInfo.TryGetValue(s.CurriculumLessonId ?? Guid.Empty, out var li);
            return new ScheduleCardDto(
                s.Id, classId, classTitle, s.SessionDate, s.StartsAt, s.EndsAt, s.Status, s.CompletedAt,
                s.Topic, s.MeetingUrl, s.CurriculumLessonId,
                li is null ? null : (int?)li.UnitOrder, li?.UnitTitle,
                li is null ? null : (int?)li.LessonOrder, li?.LessonTitle,
                li is null ? null : (CurriculumLessonType?)li.LessonType,
                matCounts.GetValueOrDefault(s.Id),
                li is not null && li.HomeworkPlaceholder != null ? 1 : 0);
        }).ToList();
    }
}
