using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Curriculum;

public sealed class CurriculumHandlers(IApplicationDbContext db) :
    IRequestHandler<GetCurriculumTemplatesQuery, Result<IReadOnlyCollection<CurriculumTemplateSummaryDto>>>,
    IRequestHandler<GetCurriculumTreeQuery, Result<CurriculumTreeDto>>,
    IRequestHandler<AssignCurriculumToClassCommand, Result<ClassCurriculumDto>>,
    IRequestHandler<GetClassCurriculumQuery, Result<ClassCurriculumDto>>
{
    public async Task<Result<IReadOnlyCollection<CurriculumTemplateSummaryDto>>> Handle(
        GetCurriculumTemplatesQuery request, CancellationToken ct)
    {
        var q = db.CurriculumTemplates.AsNoTracking().Where(t => t.IsPublished);
        if (request.Category is { } cat) q = q.Where(t => t.Category == cat);

        var items = await q
            .OrderBy(t => t.Category).ThenBy(t => t.Name)
            .Select(t => new CurriculumTemplateSummaryDto(
                t.Id, t.Name, t.Category, t.Level, t.Description, t.IsSystem,
                db.CurriculumModules.Count(m => m.TemplateId == t.Id),
                db.CurriculumUnits.Count(u => db.CurriculumModules.Any(m => m.Id == u.ModuleId && m.TemplateId == t.Id)),
                db.CurriculumLessons.Count(l => db.CurriculumUnits.Any(u =>
                    u.Id == l.UnitId && db.CurriculumModules.Any(m => m.Id == u.ModuleId && m.TemplateId == t.Id)))))
            .ToListAsync(ct);

        return Result<IReadOnlyCollection<CurriculumTemplateSummaryDto>>.Ok(items);
    }

    public async Task<Result<CurriculumTreeDto>> Handle(GetCurriculumTreeQuery request, CancellationToken ct)
    {
        var t = await db.CurriculumTemplates.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.TemplateId, ct);
        if (t is null) return Result<CurriculumTreeDto>.Fail("NOT_FOUND", "Template not found.");

        var modules = await db.CurriculumModules.AsNoTracking()
            .Where(m => m.TemplateId == t.Id).OrderBy(m => m.Order)
            .Select(m => new { m.Id, m.Order, m.Title }).ToListAsync(ct);
        var moduleIds = modules.Select(m => m.Id).ToList();
        var units = await db.CurriculumUnits.AsNoTracking()
            .Where(u => moduleIds.Contains(u.ModuleId)).OrderBy(u => u.Order)
            .Select(u => new { u.Id, u.ModuleId, u.Order, u.Title }).ToListAsync(ct);
        var unitIds = units.Select(u => u.Id).ToList();
        // Keep UnitId alongside each lesson DTO so we can group by unit in memory.
        var lessonsByUnit = await db.CurriculumLessons.AsNoTracking()
            .Where(l => unitIds.Contains(l.UnitId)).OrderBy(l => l.Order)
            .Select(l => new
            {
                l.UnitId,
                Dto = new CurriculumLessonDto(l.Id, l.Order, l.Title, l.Objectives,
                    l.HomeworkPlaceholder, l.MaterialsPlaceholder, l.IsAssessment),
            })
            .ToListAsync(ct);

        var tree = new CurriculumTreeDto(t.Id, t.Name, t.Category, t.Level, t.Description, t.IsSystem,
            modules.Select(m => new CurriculumModuleDto(m.Id, m.Order, m.Title,
                units.Where(u => u.ModuleId == m.Id)
                    .Select(u => new CurriculumUnitDto(u.Id, u.Order, u.Title,
                        lessonsByUnit.Where(x => x.UnitId == u.Id).Select(x => x.Dto).ToList()))
                    .ToList())).ToList());

        return Result<CurriculumTreeDto>.Ok(tree);
    }

    public async Task<Result<ClassCurriculumDto>> Handle(AssignCurriculumToClassCommand request, CancellationToken ct)
    {
        var cls = await db.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId, ct);
        if (cls is null) return Result<ClassCurriculumDto>.Fail("NOT_FOUND", "Class not found.");

        var template = await db.CurriculumTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == request.TemplateId, ct);
        if (template is null) return Result<ClassCurriculumDto>.Fail("NOT_FOUND", "Template not found.");

        cls.SetCurriculumTemplate(template.Id);

        // Lessons in curriculum order (module→unit→lesson).
        var orderedLessons = await (
            from l in db.CurriculumLessons.AsNoTracking()
            join u in db.CurriculumUnits.AsNoTracking() on l.UnitId equals u.Id
            join m in db.CurriculumModules.AsNoTracking() on u.ModuleId equals m.Id
            where m.TemplateId == template.Id
            orderby m.Order, u.Order, l.Order
            select new { l.Id, l.Title }).ToListAsync(ct);

        // Upcoming sessions (today forward) in date order — these get the lessons.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var upcoming = await db.ClassSessions
            .Where(s => s.ClassId == cls.Id && s.SessionDate >= today)
            .OrderBy(s => s.SessionDate).ThenBy(s => s.StartsAt)
            .ToListAsync(ct);

        for (var i = 0; i < upcoming.Count && i < orderedLessons.Count; i++)
            upcoming[i].LinkCurriculumLesson(orderedLessons[i].Id, orderedLessons[i].Title);

        await db.SaveChangesAsync(ct);
        return await Handle(new GetClassCurriculumQuery(cls.Id), ct);
    }

    public async Task<Result<ClassCurriculumDto>> Handle(GetClassCurriculumQuery request, CancellationToken ct)
    {
        var cls = await db.Classes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ClassId, ct);
        if (cls is null) return Result<ClassCurriculumDto>.Fail("NOT_FOUND", "Class not found.");

        string? templateName = null;
        if (cls.CurriculumTemplateId is { } tid)
            templateName = await db.CurriculumTemplates.AsNoTracking()
                .Where(t => t.Id == tid).Select(t => t.Name).FirstOrDefaultAsync(ct);

        var sessions = await db.ClassSessions.AsNoTracking()
            .Where(s => s.ClassId == request.ClassId)
            .OrderBy(s => s.SessionDate).ThenBy(s => s.StartsAt)
            .Select(s => new { s.Id, s.SessionDate, s.StartsAt, s.EndsAt, s.Topic, s.CurriculumLessonId })
            .ToListAsync(ct);

        var lessonIds = sessions.Where(s => s.CurriculumLessonId != null)
            .Select(s => s.CurriculumLessonId!.Value).Distinct().ToList();
        var lessonInfo = await (
            from l in db.CurriculumLessons.AsNoTracking()
            join u in db.CurriculumUnits.AsNoTracking() on l.UnitId equals u.Id
            join m in db.CurriculumModules.AsNoTracking() on u.ModuleId equals m.Id
            where lessonIds.Contains(l.Id)
            select new { l.Id, ModuleTitle = m.Title, UnitTitle = u.Title, l.Objectives, l.IsAssessment })
            .ToListAsync(ct);
        var map = lessonInfo.ToDictionary(x => x.Id);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var schedule = sessions.Select(s =>
        {
            map.TryGetValue(s.CurriculumLessonId ?? Guid.Empty, out var li);
            return new ScheduledLessonDto(
                s.Id, s.SessionDate, s.StartsAt, s.EndsAt,
                li?.ModuleTitle, li?.UnitTitle,
                s.Topic ?? li?.UnitTitle ?? "Lesson", li?.Objectives,
                s.CurriculumLessonId, li?.IsAssessment ?? false,
                s.SessionDate < today);
        }).ToList();

        var todayDto = schedule.FirstOrDefault(x => x.Date == today);
        var nextDto = schedule.FirstOrDefault(x => x.Date > today);
        var total = schedule.Count(x => x.CurriculumLessonId != null);
        var completed = schedule.Count(x => x.CurriculumLessonId != null && x.IsPast);

        return Result<ClassCurriculumDto>.Ok(new ClassCurriculumDto(
            request.ClassId, cls.CurriculumTemplateId, templateName, total, completed, todayDto, nextDto, schedule));
    }
}
