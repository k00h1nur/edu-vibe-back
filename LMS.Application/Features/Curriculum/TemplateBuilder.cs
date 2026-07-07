using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Curriculum;

// ===== DTO (reuses the class-builder unit/lesson shapes) ====================

/// <summary>A MASTER template's editable structure (units → lessons), flattened across
/// modules like the class course builder. Every mutation returns the whole tree.</summary>
public sealed record TemplateCourseDto(
    Guid TemplateId, string Name, bool IsPublished, int ClassCount, IReadOnlyList<CourseBuilderUnitDto> Units);

// ===== Use cases (admin — edit the master template directly) ================

public sealed record GetTemplateCourseQuery(Guid TemplateId) : IRequest<Result<TemplateCourseDto>>;

public sealed record CreateTemplateUnitCommand(
    Guid TemplateId, string Title, string? Description, string? Icon, int? EstimatedMinutes, int XpReward)
    : IRequest<Result<TemplateCourseDto>>;

public sealed record UpdateTemplateUnitCommand(
    Guid UnitId, string Title, string? Description, string? Icon, int? EstimatedMinutes, int XpReward)
    : IRequest<Result<TemplateCourseDto>>;

public sealed record DeleteTemplateUnitCommand(Guid UnitId) : IRequest<Result<TemplateCourseDto>>;

public sealed record ReorderTemplateUnitsCommand(Guid TemplateId, IReadOnlyList<Guid> UnitIds)
    : IRequest<Result<TemplateCourseDto>>;

public sealed record CreateTemplateLessonCommand(
    Guid UnitId, string Title, string? Objectives, string? Homework, string? Materials,
    bool IsAssessment, CurriculumLessonType LessonType, int? DurationMinutes, int XpReward)
    : IRequest<Result<TemplateCourseDto>>;

public sealed record UpdateTemplateLessonCommand(
    Guid LessonId, string Title, string? Objectives, string? Homework, string? Materials,
    bool IsAssessment, CurriculumLessonType LessonType, int? DurationMinutes, int XpReward)
    : IRequest<Result<TemplateCourseDto>>;

public sealed record DeleteTemplateLessonCommand(Guid LessonId) : IRequest<Result<TemplateCourseDto>>;

public sealed record ReorderTemplateLessonsCommand(Guid UnitId, IReadOnlyList<Guid> LessonIds)
    : IRequest<Result<TemplateCourseDto>>;

// ===== Handlers ============================================================

public sealed class TemplateBuilderHandlers(IApplicationDbContext db) :
    IRequestHandler<GetTemplateCourseQuery, Result<TemplateCourseDto>>,
    IRequestHandler<CreateTemplateUnitCommand, Result<TemplateCourseDto>>,
    IRequestHandler<UpdateTemplateUnitCommand, Result<TemplateCourseDto>>,
    IRequestHandler<DeleteTemplateUnitCommand, Result<TemplateCourseDto>>,
    IRequestHandler<ReorderTemplateUnitsCommand, Result<TemplateCourseDto>>,
    IRequestHandler<CreateTemplateLessonCommand, Result<TemplateCourseDto>>,
    IRequestHandler<UpdateTemplateLessonCommand, Result<TemplateCourseDto>>,
    IRequestHandler<DeleteTemplateLessonCommand, Result<TemplateCourseDto>>,
    IRequestHandler<ReorderTemplateLessonsCommand, Result<TemplateCourseDto>>
{
    private static Result<TemplateCourseDto> Fail(string code, string msg) => Result<TemplateCourseDto>.Fail(code, msg);

    public async Task<Result<TemplateCourseDto>> Handle(GetTemplateCourseQuery request, CancellationToken ct)
    {
        if (!await db.CurriculumTemplates.AnyAsync(t => t.Id == request.TemplateId, ct))
            return Fail("NOT_FOUND", "Template not found.");
        return Result<TemplateCourseDto>.Ok(await BuildAsync(request.TemplateId, ct));
    }

    public async Task<Result<TemplateCourseDto>> Handle(CreateTemplateUnitCommand request, CancellationToken ct)
    {
        if (!await db.CurriculumTemplates.AnyAsync(t => t.Id == request.TemplateId, ct))
            return Fail("NOT_FOUND", "Template not found.");
        var moduleId = await EnsureModuleAsync(request.TemplateId, ct);
        var unit = new CurriculumUnit(moduleId, await NextUnitOrderAsync(request.TemplateId, ct), request.Title);
        unit.Update(request.Title, request.Description);
        unit.SetMeta(request.Icon, request.EstimatedMinutes, request.XpReward);
        await db.CurriculumUnits.AddAsync(unit, ct);
        await db.SaveChangesAsync(ct);
        return Result<TemplateCourseDto>.Ok(await BuildAsync(request.TemplateId, ct));
    }

    public async Task<Result<TemplateCourseDto>> Handle(UpdateTemplateUnitCommand request, CancellationToken ct)
    {
        var unit = await db.CurriculumUnits.FirstOrDefaultAsync(u => u.Id == request.UnitId, ct);
        if (unit is null) return Fail("NOT_FOUND", "Unit not found.");
        unit.Update(request.Title, request.Description);
        unit.SetMeta(request.Icon, request.EstimatedMinutes, request.XpReward);
        await db.SaveChangesAsync(ct);
        return Result<TemplateCourseDto>.Ok(await BuildAsync(await TemplateIdForModuleAsync(unit.ModuleId, ct), ct));
    }

    public async Task<Result<TemplateCourseDto>> Handle(DeleteTemplateUnitCommand request, CancellationToken ct)
    {
        var unit = await db.CurriculumUnits.FirstOrDefaultAsync(u => u.Id == request.UnitId, ct);
        if (unit is null) return Fail("NOT_FOUND", "Unit not found.");
        var templateId = await TemplateIdForModuleAsync(unit.ModuleId, ct);
        var guard = await GuardStructuralAsync(templateId, ct);
        if (guard is not null) return guard;
        db.CurriculumUnits.Remove(unit);
        await db.SaveChangesAsync(ct);
        return Result<TemplateCourseDto>.Ok(await BuildAsync(templateId, ct));
    }

    public async Task<Result<TemplateCourseDto>> Handle(ReorderTemplateUnitsCommand request, CancellationToken ct)
    {
        var moduleIds = await db.CurriculumModules.Where(m => m.TemplateId == request.TemplateId).Select(m => m.Id).ToListAsync(ct);
        var units = await db.CurriculumUnits.Where(u => moduleIds.Contains(u.ModuleId)).ToListAsync(ct);
        var byId = units.ToDictionary(u => u.Id);
        var order = 1;
        foreach (var id in request.UnitIds)
            if (byId.TryGetValue(id, out var u)) u.SetOrder(order++);
        await db.SaveChangesAsync(ct);
        return Result<TemplateCourseDto>.Ok(await BuildAsync(request.TemplateId, ct));
    }

    public async Task<Result<TemplateCourseDto>> Handle(CreateTemplateLessonCommand request, CancellationToken ct)
    {
        var unit = await db.CurriculumUnits.FirstOrDefaultAsync(u => u.Id == request.UnitId, ct);
        if (unit is null) return Fail("NOT_FOUND", "Unit not found.");
        var nextOrder = (await db.CurriculumLessons.Where(l => l.UnitId == unit.Id).MaxAsync(l => (int?)l.Order, ct) ?? 0) + 1;
        var lesson = new CurriculumLesson(unit.Id, nextOrder, request.Title,
            request.Objectives, request.Homework, request.Materials, request.IsAssessment);
        lesson.SetMeta(request.LessonType, request.DurationMinutes, request.XpReward);
        await db.CurriculumLessons.AddAsync(lesson, ct);
        await db.SaveChangesAsync(ct);
        return Result<TemplateCourseDto>.Ok(await BuildAsync(await TemplateIdForModuleAsync(unit.ModuleId, ct), ct));
    }

    public async Task<Result<TemplateCourseDto>> Handle(UpdateTemplateLessonCommand request, CancellationToken ct)
    {
        var lesson = await db.CurriculumLessons.FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);
        if (lesson is null) return Fail("NOT_FOUND", "Lesson not found.");
        lesson.Update(request.Title, request.Objectives, request.Homework, request.Materials, request.IsAssessment);
        lesson.SetMeta(request.LessonType, request.DurationMinutes, request.XpReward);
        await db.SaveChangesAsync(ct);
        return Result<TemplateCourseDto>.Ok(await BuildAsync(await TemplateIdForLessonAsync(lesson.Id, ct), ct));
    }

    public async Task<Result<TemplateCourseDto>> Handle(DeleteTemplateLessonCommand request, CancellationToken ct)
    {
        var lesson = await db.CurriculumLessons.FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);
        if (lesson is null) return Fail("NOT_FOUND", "Lesson not found.");
        var templateId = await TemplateIdForLessonAsync(lesson.Id, ct);
        var guard = await GuardStructuralAsync(templateId, ct);
        if (guard is not null) return guard;
        db.CurriculumLessons.Remove(lesson);
        await db.SaveChangesAsync(ct);
        return Result<TemplateCourseDto>.Ok(await BuildAsync(templateId, ct));
    }

    public async Task<Result<TemplateCourseDto>> Handle(ReorderTemplateLessonsCommand request, CancellationToken ct)
    {
        var lessons = await db.CurriculumLessons.Where(l => l.UnitId == request.UnitId).ToListAsync(ct);
        var byId = lessons.ToDictionary(l => l.Id);
        var order = 1;
        foreach (var id in request.LessonIds)
            if (byId.TryGetValue(id, out var l)) l.SetOrder(order++);
        await db.SaveChangesAsync(ct);
        var templateId = await TemplateIdForUnitAsync(request.UnitId, ct);
        return Result<TemplateCourseDto>.Ok(await BuildAsync(templateId, ct));
    }

    // ---- helpers -----------------------------------------------------------

    /// <summary>Structural edits (delete unit/lesson) are blocked while any class uses
    /// this template directly — cascading a delete would break those classes and destroy
    /// students' exercise answers. Add/edit/reorder stay allowed.</summary>
    private async Task<Result<TemplateCourseDto>?> GuardStructuralAsync(Guid templateId, CancellationToken ct)
    {
        var used = await db.Classes.CountAsync(c => c.CurriculumTemplateId == templateId, ct);
        return used > 0
            ? Fail("IN_USE", $"This template is used by {used} class(es). Unassign it there before deleting units/lessons.")
            : null;
    }

    private async Task<Guid> EnsureModuleAsync(Guid templateId, CancellationToken ct)
    {
        var moduleId = await db.CurriculumModules.Where(m => m.TemplateId == templateId)
            .OrderBy(m => m.Order).Select(m => (Guid?)m.Id).FirstOrDefaultAsync(ct);
        if (moduleId is { } id) return id;
        var module = new CurriculumModule(templateId, 1, "Course");
        await db.CurriculumModules.AddAsync(module, ct);
        await db.SaveChangesAsync(ct);
        return module.Id;
    }

    private async Task<int> NextUnitOrderAsync(Guid templateId, CancellationToken ct)
    {
        var moduleIds = await db.CurriculumModules.Where(m => m.TemplateId == templateId).Select(m => m.Id).ToListAsync(ct);
        return (await db.CurriculumUnits.Where(u => moduleIds.Contains(u.ModuleId)).MaxAsync(u => (int?)u.Order, ct) ?? 0) + 1;
    }

    private Task<Guid> TemplateIdForModuleAsync(Guid moduleId, CancellationToken ct) =>
        db.CurriculumModules.Where(m => m.Id == moduleId).Select(m => m.TemplateId).FirstAsync(ct);

    private Task<Guid> TemplateIdForUnitAsync(Guid unitId, CancellationToken ct) =>
        db.CurriculumUnits.Where(u => u.Id == unitId)
            .Join(db.CurriculumModules, u => u.ModuleId, m => m.Id, (u, m) => m.TemplateId).FirstAsync(ct);

    private Task<Guid> TemplateIdForLessonAsync(Guid lessonId, CancellationToken ct) =>
        db.CurriculumLessons.Where(l => l.Id == lessonId)
            .Join(db.CurriculumUnits, l => l.UnitId, u => u.Id, (l, u) => u.ModuleId)
            .Join(db.CurriculumModules, mid => mid, m => m.Id, (mid, m) => m.TemplateId).FirstAsync(ct);

    private async Task<TemplateCourseDto> BuildAsync(Guid templateId, CancellationToken ct)
    {
        var t = await db.CurriculumTemplates.Where(x => x.Id == templateId)
            .Select(x => new { x.Name, x.IsPublished }).FirstAsync(ct);
        var classCount = await db.Classes.CountAsync(c => c.CurriculumTemplateId == templateId, ct);

        var moduleIds = await db.CurriculumModules.Where(m => m.TemplateId == templateId).Select(m => m.Id).ToListAsync(ct);
        var units = await db.CurriculumUnits.AsNoTracking()
            .Where(u => moduleIds.Contains(u.ModuleId)).OrderBy(u => u.Order)
            .Select(u => new { u.Id, u.Order, u.Title, u.Description, u.Icon, u.EstimatedMinutes, u.XpReward }).ToListAsync(ct);
        var unitIds = units.Select(u => u.Id).ToList();
        var lessons = await db.CurriculumLessons.AsNoTracking()
            .Where(l => unitIds.Contains(l.UnitId)).OrderBy(l => l.Order)
            .Select(l => new { l.UnitId, l.Id, l.Order, l.Title, l.Objectives, l.HomeworkPlaceholder, l.MaterialsPlaceholder, l.IsAssessment, l.LessonType, l.DurationMinutes, l.XpReward })
            .ToListAsync(ct);

        // Self-check exercise count per lesson — surfaced as a badge so the teacher
        // can see which lessons already have exercises (and confirm a save landed).
        var lessonIds = lessons.Select(l => l.Id).ToList();
        var exerciseCounts = (await db.LessonExercises.AsNoTracking()
                .Where(e => lessonIds.Contains(e.LessonId))
                .GroupBy(e => e.LessonId)
                .Select(g => new { LessonId = g.Key, Count = g.Count() })
                .ToListAsync(ct))
            .ToDictionary(x => x.LessonId, x => x.Count);

        var unitDtos = units.Select(u =>
        {
            var ls = lessons.Where(x => x.UnitId == u.Id).ToList();
            return new CourseBuilderUnitDto(
                u.Id, u.Order, u.Title, u.Description, u.Icon, u.EstimatedMinutes, u.XpReward,
                ls.Count,
                ls.Count(x => x.MaterialsPlaceholder != null),
                ls.Count(x => x.HomeworkPlaceholder != null),
                ls.Count(x => x.IsAssessment),
                u.XpReward + ls.Sum(x => x.XpReward),
                ls.Select(x => new CourseBuilderLessonDto(x.Id, x.Order, x.Title, x.Objectives,
                    x.HomeworkPlaceholder, x.MaterialsPlaceholder, x.IsAssessment,
                    x.LessonType, x.DurationMinutes, x.XpReward, exerciseCounts.GetValueOrDefault(x.Id))).ToList());
        }).ToList();

        return new TemplateCourseDto(templateId, t.Name, t.IsPublished, classCount, unitDtos);
    }
}
