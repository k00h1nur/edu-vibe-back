using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Curriculum;

/// <summary>
/// Teacher Course Builder. Operates on the class's OWN editable curriculum
/// template (lazily created the first time the builder opens) so a teacher can
/// structure units + lessons without touching shared system templates. Every
/// call self-scopes to the class teacher (or an admin).
/// </summary>
public sealed class CourseBuilderHandlers(IApplicationDbContext db, ICurrentUserService currentUser) :
    IRequestHandler<GetClassCourseBuilderQuery, Result<ClassCourseBuilderDto>>,
    IRequestHandler<CreateCourseUnitCommand, Result<ClassCourseBuilderDto>>,
    IRequestHandler<UpdateCourseUnitCommand, Result<ClassCourseBuilderDto>>,
    IRequestHandler<DeleteCourseUnitCommand, Result<ClassCourseBuilderDto>>,
    IRequestHandler<ReorderCourseUnitsCommand, Result<ClassCourseBuilderDto>>,
    IRequestHandler<CreateCourseLessonCommand, Result<ClassCourseBuilderDto>>,
    IRequestHandler<UpdateCourseLessonCommand, Result<ClassCourseBuilderDto>>,
    IRequestHandler<DeleteCourseLessonCommand, Result<ClassCourseBuilderDto>>,
    IRequestHandler<ReorderCourseLessonsCommand, Result<ClassCourseBuilderDto>>
{
    // ---- public handlers --------------------------------------------------

    public async Task<Result<ClassCourseBuilderDto>> Handle(GetClassCourseBuilderQuery request, CancellationToken ct)
        => await WithCourse(request.ClassId, ct, (_, _) => Task.CompletedTask);

    public async Task<Result<ClassCourseBuilderDto>> Handle(CreateCourseUnitCommand request, CancellationToken ct)
        => await WithCourse(request.ClassId, ct, async (templateId, moduleId) =>
        {
            var nextOrder = await NextUnitOrder(templateId, ct);
            var unit = new CurriculumUnit(moduleId, nextOrder, request.Title);
            unit.Update(request.Title, request.Description);
            await db.CurriculumUnits.AddAsync(unit, ct);
            await db.SaveChangesAsync(ct);
        });

    public async Task<Result<ClassCourseBuilderDto>> Handle(UpdateCourseUnitCommand request, CancellationToken ct)
    {
        var unit = await db.CurriculumUnits.FirstOrDefaultAsync(u => u.Id == request.UnitId, ct);
        if (unit is null) return Fail("NOT_FOUND", "Unit not found.");
        var classId = await ClassIdForModule(unit.ModuleId, ct);
        return await WithCourse(classId, ct, async (_, _) =>
        {
            unit.Update(request.Title, request.Description);
            await db.SaveChangesAsync(ct);
        });
    }

    public async Task<Result<ClassCourseBuilderDto>> Handle(DeleteCourseUnitCommand request, CancellationToken ct)
    {
        var unit = await db.CurriculumUnits.FirstOrDefaultAsync(u => u.Id == request.UnitId, ct);
        if (unit is null) return Fail("NOT_FOUND", "Unit not found.");
        var classId = await ClassIdForModule(unit.ModuleId, ct);
        return await WithCourse(classId, ct, async (_, _) =>
        {
            db.CurriculumUnits.Remove(unit); // cascade removes its lessons
            await db.SaveChangesAsync(ct);
        });
    }

    public async Task<Result<ClassCourseBuilderDto>> Handle(ReorderCourseUnitsCommand request, CancellationToken ct)
        => await WithCourse(request.ClassId, ct, async (templateId, _) =>
        {
            var moduleIds = await db.CurriculumModules.Where(m => m.TemplateId == templateId)
                .Select(m => m.Id).ToListAsync(ct);
            var units = await db.CurriculumUnits.Where(u => moduleIds.Contains(u.ModuleId)).ToListAsync(ct);
            for (var i = 0; i < request.UnitIds.Count; i++)
                units.FirstOrDefault(u => u.Id == request.UnitIds[i])?.SetOrder(i + 1);
            await db.SaveChangesAsync(ct);
        });

    public async Task<Result<ClassCourseBuilderDto>> Handle(CreateCourseLessonCommand request, CancellationToken ct)
    {
        var unit = await db.CurriculumUnits.FirstOrDefaultAsync(u => u.Id == request.UnitId, ct);
        if (unit is null) return Fail("NOT_FOUND", "Unit not found.");
        var classId = await ClassIdForModule(unit.ModuleId, ct);
        return await WithCourse(classId, ct, async (_, _) =>
        {
            var nextOrder = (await db.CurriculumLessons.Where(l => l.UnitId == unit.Id)
                .MaxAsync(l => (int?)l.Order, ct) ?? 0) + 1;
            var lesson = new CurriculumLesson(unit.Id, nextOrder, request.Title,
                request.Objectives, request.Homework, request.Materials, request.IsAssessment);
            await db.CurriculumLessons.AddAsync(lesson, ct);
            await db.SaveChangesAsync(ct);
        });
    }

    public async Task<Result<ClassCourseBuilderDto>> Handle(UpdateCourseLessonCommand request, CancellationToken ct)
    {
        var lesson = await db.CurriculumLessons.FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);
        if (lesson is null) return Fail("NOT_FOUND", "Lesson not found.");
        var classId = await ClassIdForUnit(lesson.UnitId, ct);
        return await WithCourse(classId, ct, async (_, _) =>
        {
            lesson.Update(request.Title, request.Objectives, request.Homework, request.Materials, request.IsAssessment);
            await db.SaveChangesAsync(ct);
        });
    }

    public async Task<Result<ClassCourseBuilderDto>> Handle(DeleteCourseLessonCommand request, CancellationToken ct)
    {
        var lesson = await db.CurriculumLessons.FirstOrDefaultAsync(l => l.Id == request.LessonId, ct);
        if (lesson is null) return Fail("NOT_FOUND", "Lesson not found.");
        var classId = await ClassIdForUnit(lesson.UnitId, ct);
        return await WithCourse(classId, ct, async (_, _) =>
        {
            db.CurriculumLessons.Remove(lesson);
            await db.SaveChangesAsync(ct);
        });
    }

    public async Task<Result<ClassCourseBuilderDto>> Handle(ReorderCourseLessonsCommand request, CancellationToken ct)
    {
        var unit = await db.CurriculumUnits.FirstOrDefaultAsync(u => u.Id == request.UnitId, ct);
        if (unit is null) return Fail("NOT_FOUND", "Unit not found.");
        var classId = await ClassIdForModule(unit.ModuleId, ct);
        return await WithCourse(classId, ct, async (_, _) =>
        {
            var lessons = await db.CurriculumLessons.Where(l => l.UnitId == unit.Id).ToListAsync(ct);
            for (var i = 0; i < request.LessonIds.Count; i++)
                lessons.FirstOrDefault(l => l.Id == request.LessonIds[i])?.SetOrder(i + 1);
            await db.SaveChangesAsync(ct);
        });
    }

    // ---- shared plumbing --------------------------------------------------

    private static Result<ClassCourseBuilderDto> Fail(string code, string msg) =>
        Result<ClassCourseBuilderDto>.Fail(code, msg);

    private bool IsAdmin =>
        currentUser.IsInRole(RoleCodes.Admin) || currentUser.IsInRole(RoleCodes.SuperAdmin)
        || currentUser.IsInRole(RoleCodes.OfficeAdmin);

    /// <summary>
    /// Authorizes the caller for the class, ensures the class has its own
    /// editable template + default module, runs the supplied mutation, then
    /// returns the refreshed builder tree.
    /// </summary>
    private async Task<Result<ClassCourseBuilderDto>> WithCourse(
        Guid classId, CancellationToken ct, Func<Guid, Guid, Task> mutate)
    {
        var cls = await db.Classes.FirstOrDefaultAsync(c => c.Id == classId, ct);
        if (cls is null) return Fail("NOT_FOUND", "Class not found.");
        if (!IsAdmin && (cls.TeacherUserId is null || cls.TeacherUserId != currentUser.UserId))
            return Fail("FORBIDDEN", "Only the class teacher or an admin can edit this course.");

        var (templateId, moduleId) = await EnsureClassCourseAsync(cls, ct);
        await mutate(templateId, moduleId);
        return await BuildDtoAsync(classId, templateId, ct);
    }

    /// <summary>Gives the class its own editable (non-system) template + a default module if it has none.</summary>
    private async Task<(Guid TemplateId, Guid ModuleId)> EnsureClassCourseAsync(Class cls, CancellationToken ct)
    {
        Guid templateId;
        if (cls.CurriculumTemplateId is { } tid &&
            await db.CurriculumTemplates.AnyAsync(t => t.Id == tid && !t.IsSystem, ct))
        {
            templateId = tid;
        }
        else
        {
            var template = new CurriculumTemplate(
                $"{cls.Title} — Course", CurriculumCategory.Custom, null, null, isSystem: false);
            await db.CurriculumTemplates.AddAsync(template, ct);
            await db.SaveChangesAsync(ct);
            cls.SetCurriculumTemplate(template.Id);
            await db.SaveChangesAsync(ct);
            templateId = template.Id;
        }

        var module = await db.CurriculumModules
            .Where(m => m.TemplateId == templateId).OrderBy(m => m.Order)
            .FirstOrDefaultAsync(ct);
        if (module is null)
        {
            module = new CurriculumModule(templateId, 1, "Course");
            await db.CurriculumModules.AddAsync(module, ct);
            await db.SaveChangesAsync(ct);
        }
        return (templateId, module.Id);
    }

    private async Task<int> NextUnitOrder(Guid templateId, CancellationToken ct)
    {
        var moduleIds = await db.CurriculumModules.Where(m => m.TemplateId == templateId).Select(m => m.Id).ToListAsync(ct);
        return (await db.CurriculumUnits.Where(u => moduleIds.Contains(u.ModuleId))
            .MaxAsync(u => (int?)u.Order, ct) ?? 0) + 1;
    }

    private async Task<Guid> ClassIdForModule(Guid moduleId, CancellationToken ct) =>
        await db.CurriculumModules.Where(m => m.Id == moduleId)
            .Join(db.Classes, m => m.TemplateId, c => c.CurriculumTemplateId, (m, c) => c.Id)
            .FirstOrDefaultAsync(ct);

    private async Task<Guid> ClassIdForUnit(Guid unitId, CancellationToken ct)
    {
        var moduleId = await db.CurriculumUnits.Where(u => u.Id == unitId).Select(u => u.ModuleId).FirstOrDefaultAsync(ct);
        return await ClassIdForModule(moduleId, ct);
    }

    private async Task<Result<ClassCourseBuilderDto>> BuildDtoAsync(Guid classId, Guid templateId, CancellationToken ct)
    {
        var templateName = await db.CurriculumTemplates.Where(t => t.Id == templateId)
            .Select(t => t.Name).FirstAsync(ct);

        var moduleIds = await db.CurriculumModules.Where(m => m.TemplateId == templateId).Select(m => m.Id).ToListAsync(ct);
        var units = await db.CurriculumUnits.AsNoTracking()
            .Where(u => moduleIds.Contains(u.ModuleId)).OrderBy(u => u.Order)
            .Select(u => new { u.Id, u.Order, u.Title, u.Description }).ToListAsync(ct);
        var unitIds = units.Select(u => u.Id).ToList();
        var lessons = await db.CurriculumLessons.AsNoTracking()
            .Where(l => unitIds.Contains(l.UnitId)).OrderBy(l => l.Order)
            .Select(l => new { l.UnitId, l.Id, l.Order, l.Title, l.Objectives, l.HomeworkPlaceholder, l.MaterialsPlaceholder, l.IsAssessment })
            .ToListAsync(ct);

        var unitDtos = units.Select(u =>
        {
            var ls = lessons.Where(x => x.UnitId == u.Id).ToList();
            return new CourseBuilderUnitDto(
                u.Id, u.Order, u.Title, u.Description,
                ls.Count,
                ls.Count(x => x.MaterialsPlaceholder != null),
                ls.Count(x => x.HomeworkPlaceholder != null),
                ls.Count(x => x.IsAssessment),
                ls.Select(x => new CurriculumLessonDto(x.Id, x.Order, x.Title, x.Objectives,
                    x.HomeworkPlaceholder, x.MaterialsPlaceholder, x.IsAssessment)).ToList());
        }).ToList();

        return Result<ClassCourseBuilderDto>.Ok(new ClassCourseBuilderDto(classId, templateId, templateName, unitDtos));
    }
}
