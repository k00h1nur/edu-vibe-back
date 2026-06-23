using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Curriculum;

// ---- Student Roadmap DTOs -------------------------------------------------

/// <summary>A lesson on the student's path — its kind, reward, and completion (derived from the linked session).</summary>
public sealed record RoadmapLessonDto(
    Guid Id, int Order, string Title, CurriculumLessonType LessonType,
    int? DurationMinutes, int XpReward, bool IsAssessment,
    bool HasMaterials, bool HasHomework,
    bool Completed, DateOnly? CompletedOn, DateOnly? ScheduledDate, Guid? SessionId);

/// <summary>A unit node on the path — progress, XP and a state (completed / current / locked).</summary>
public sealed record RoadmapUnitDto(
    Guid Id, int Order, string Title, string? Description, string? Icon,
    int? EstimatedMinutes, int TotalLessons, int CompletedLessons,
    int TotalXp, int EarnedXp, int ProgressPct, string State, string? LockedReason,
    IReadOnlyList<RoadmapLessonDto> Lessons);

/// <summary>A class's curriculum as a student learning journey (Duolingo-style path).</summary>
public sealed record StudentRoadmapDto(
    Guid ClassId, string ClassTitle, Guid? TemplateId, string? TemplateName,
    int TotalUnits, int CompletedUnits, int TotalLessons, int CompletedLessons,
    int TotalXp, int EarnedXp, int ProgressPct,
    Guid? CurrentUnitId, RoadmapLessonDto? CurrentLesson, RoadmapLessonDto? NextLesson,
    IReadOnlyList<RoadmapUnitDto> Units);

public sealed record GetStudentRoadmapQuery(Guid ClassId) : IRequest<Result<StudentRoadmapDto>>;

// ---- Handler --------------------------------------------------------------

/// <summary>
/// Builds the student learning roadmap for a class. Completion is derived from
/// real data — a lesson is "completed" when a <see cref="ClassSession"/> linked
/// to it (<c>CurriculumLessonId</c>) has already happened. Readable by the
/// enrolled student, the class teacher, or an admin.
/// </summary>
public sealed class StudentRoadmapHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetStudentRoadmapQuery, Result<StudentRoadmapDto>>
{
    private const string Completed = "completed", Current = "current", Locked = "locked";

    private bool IsAdmin =>
        currentUser.IsInRole(RoleCodes.Admin) || currentUser.IsInRole(RoleCodes.SuperAdmin)
        || currentUser.IsInRole(RoleCodes.OfficeAdmin);

    public async Task<Result<StudentRoadmapDto>> Handle(GetStudentRoadmapQuery request, CancellationToken ct)
    {
        var cls = await db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ClassId, ct);
        if (cls is null) return Result<StudentRoadmapDto>.Fail("NOT_FOUND", "Class not found.");
        if (!await CanViewAsync(cls, ct))
            return Result<StudentRoadmapDto>.Fail("FORBIDDEN", "You don't have access to this class.");

        string? templateName = null;
        if (cls.CurriculumTemplateId is { } tid)
            templateName = await db.CurriculumTemplates.AsNoTracking()
                .Where(t => t.Id == tid).Select(t => t.Name).FirstOrDefaultAsync(ct);

        // No curriculum bound yet → an empty (but valid) roadmap.
        if (cls.CurriculumTemplateId is not { } templateId)
            return Result<StudentRoadmapDto>.Ok(new StudentRoadmapDto(
                cls.Id, cls.Title, null, null, 0, 0, 0, 0, 0, 0, 0, null, null, null, Array.Empty<RoadmapUnitDto>()));

        var moduleIds = await db.CurriculumModules.AsNoTracking()
            .Where(m => m.TemplateId == templateId).Select(m => m.Id).ToListAsync(ct);
        var units = await db.CurriculumUnits.AsNoTracking()
            .Where(u => moduleIds.Contains(u.ModuleId)).OrderBy(u => u.Order)
            .Select(u => new { u.Id, u.Order, u.Title, u.Description, u.Icon, u.EstimatedMinutes, u.XpReward })
            .ToListAsync(ct);
        var unitIds = units.Select(u => u.Id).ToList();
        var lessons = await db.CurriculumLessons.AsNoTracking()
            .Where(l => unitIds.Contains(l.UnitId)).OrderBy(l => l.Order)
            .Select(l => new
            {
                l.UnitId, l.Id, l.Order, l.Title, l.LessonType, l.DurationMinutes, l.XpReward, l.IsAssessment,
                HasMaterials = l.MaterialsPlaceholder != null, HasHomework = l.HomeworkPlaceholder != null,
            })
            .ToListAsync(ct);

        // Completion comes from the schedule: a lesson is done if a linked session is in the past.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var sessions = await db.ClassSessions.AsNoTracking()
            .Where(s => s.ClassId == request.ClassId && s.CurriculumLessonId != null)
            .Select(s => new { LessonId = s.CurriculumLessonId!.Value, s.SessionDate, s.Id })
            .ToListAsync(ct);
        var sessionByLesson = sessions.GroupBy(s => s.LessonId).ToDictionary(g => g.Key, g => new
        {
            Scheduled = (DateOnly?)g.Min(x => x.SessionDate),
            Completed = g.Any(x => x.SessionDate < today),
            CompletedOn = g.Where(x => x.SessionDate < today).Select(x => (DateOnly?)x.SessionDate).Max(),
            SessionId = (Guid?)g.OrderBy(x => x.SessionDate).First().Id,
        });

        // Pass 1 — per-unit lesson DTOs + aggregates.
        var agg = units.Select(u =>
        {
            var ls = lessons.Where(l => l.UnitId == u.Id).Select(l =>
            {
                sessionByLesson.TryGetValue(l.Id, out var sx);
                return new RoadmapLessonDto(
                    l.Id, l.Order, l.Title, l.LessonType, l.DurationMinutes, l.XpReward, l.IsAssessment,
                    l.HasMaterials, l.HasHomework,
                    sx?.Completed ?? false, sx?.CompletedOn, sx?.Scheduled, sx?.SessionId);
            }).ToList();
            var total = ls.Count;
            var done = ls.Count(x => x.Completed);
            var totalXp = u.XpReward + ls.Sum(x => x.XpReward);
            var earnedXp = ls.Where(x => x.Completed).Sum(x => x.XpReward) + (total > 0 && done == total ? u.XpReward : 0);
            var pct = total == 0 ? 0 : (int)Math.Round(done * 100.0 / total);
            return new { u, ls, total, done, totalXp, earnedXp, pct };
        }).ToList();

        // Pass 2 — assign the path state. First not-yet-complete unit is "current"; the rest lock behind it.
        var unitDtos = new List<RoadmapUnitDto>(agg.Count);
        var currentSeen = false;
        for (var i = 0; i < agg.Count; i++)
        {
            var a = agg[i];
            string state;
            string? locked = null;
            if (a.total > 0 && a.pct == 100) state = Completed;
            else if (!currentSeen) { state = Current; currentSeen = true; }
            else { state = Locked; locked = $"Complete “{agg[i - 1].u.Title}” to unlock."; }
            unitDtos.Add(new RoadmapUnitDto(
                a.u.Id, a.u.Order, a.u.Title, a.u.Description, a.u.Icon, a.u.EstimatedMinutes,
                a.total, a.done, a.totalXp, a.earnedXp, a.pct, state, locked, a.ls));
        }

        var flat = unitDtos.SelectMany(u => u.Lessons).ToList();
        var currentLesson = flat.FirstOrDefault(l => !l.Completed);
        RoadmapLessonDto? nextLesson = null;
        if (currentLesson is not null)
        {
            var idx = flat.FindIndex(l => l.Id == currentLesson.Id);
            nextLesson = idx >= 0 && idx + 1 < flat.Count ? flat[idx + 1] : null;
        }

        var totalLessons = agg.Sum(x => x.total);
        var completedLessons = agg.Sum(x => x.done);
        var dto = new StudentRoadmapDto(
            cls.Id, cls.Title, templateId, templateName,
            unitDtos.Count, unitDtos.Count(u => u.State == Completed),
            totalLessons, completedLessons,
            agg.Sum(x => x.totalXp), agg.Sum(x => x.earnedXp),
            totalLessons == 0 ? 0 : (int)Math.Round(completedLessons * 100.0 / totalLessons),
            unitDtos.FirstOrDefault(u => u.State == Current)?.Id, currentLesson, nextLesson, unitDtos);

        return Result<StudentRoadmapDto>.Ok(dto);
    }

    /// <summary>Admin, the class's teacher, or a student enrolled in the class may read the roadmap.</summary>
    private async Task<bool> CanViewAsync(Class cls, CancellationToken ct)
    {
        if (IsAdmin) return true;
        var uid = currentUser.UserId;
        if (uid is null) return false;
        if (cls.TeacherUserId == uid) return true;

        var profileId = await db.StudentProfiles.AsNoTracking()
            .Where(p => p.UserId == uid).Select(p => p.Id).FirstOrDefaultAsync(ct);
        if (profileId == Guid.Empty) return false;
        return await db.Enrollments.AsNoTracking()
            .AnyAsync(e => e.ClassId == cls.Id && e.StudentProfileId == profileId, ct);
    }
}
