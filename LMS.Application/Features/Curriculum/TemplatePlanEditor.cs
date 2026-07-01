using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Curriculum;

// ---- Template teaching-plan EDITING (admin day-plan builder) -------------------
//
// Defines which curriculum lessons form each "class day" on a template
// (Day 1 = 1A + 1B, an exam day = a single exam …). Every command returns the
// full refreshed <see cref="TemplatePlanDto"/> so the UI re-renders from one
// round-trip. These edits only touch curriculum_plan_days /
// curriculum_plan_day_lessons — pure grouping links, never lesson content or a
// student's submissions — so no destructive-delete guard is needed. Re-grouping
// does change what a live class's day-view pairs together, which is the point.

/// <summary>Append a new (empty) day to the end of a template's plan.</summary>
public sealed record AddPlanDayCommand(Guid TemplateId, string? Title) : IRequest<Result<TemplatePlanDto>>;

/// <summary>Rename a plan-day (null/blank clears the custom title).</summary>
public sealed record RenamePlanDayCommand(Guid PlanDayId, string? Title) : IRequest<Result<TemplatePlanDto>>;

/// <summary>Delete a plan-day and its lesson links (the lessons themselves are untouched).</summary>
public sealed record DeletePlanDayCommand(Guid PlanDayId) : IRequest<Result<TemplatePlanDto>>;

/// <summary>Set the day order for a template — dayIds in the desired top-to-bottom order.</summary>
public sealed record ReorderPlanDaysCommand(Guid TemplateId, IReadOnlyList<Guid> DayIds)
    : IRequest<Result<TemplatePlanDto>>;

/// <summary>Replace a day's lessons wholesale (in-day order = list order). Empty list clears the day.</summary>
public sealed record SetPlanDayLessonsCommand(Guid PlanDayId, IReadOnlyList<Guid> LessonIds)
    : IRequest<Result<TemplatePlanDto>>;

public sealed class TemplatePlanEditorHandlers(IApplicationDbContext db) :
    IRequestHandler<AddPlanDayCommand, Result<TemplatePlanDto>>,
    IRequestHandler<RenamePlanDayCommand, Result<TemplatePlanDto>>,
    IRequestHandler<DeletePlanDayCommand, Result<TemplatePlanDto>>,
    IRequestHandler<ReorderPlanDaysCommand, Result<TemplatePlanDto>>,
    IRequestHandler<SetPlanDayLessonsCommand, Result<TemplatePlanDto>>
{
    public async Task<Result<TemplatePlanDto>> Handle(AddPlanDayCommand request, CancellationToken ct)
    {
        if (!await db.CurriculumTemplates.AnyAsync(t => t.Id == request.TemplateId, ct))
            return Result<TemplatePlanDto>.Fail("NOT_FOUND", "Template not found.");

        return await db.ExecuteInTransactionAsync(async () =>
        {
            var maxOrder = await db.CurriculumPlanDays
                .Where(d => d.TemplateId == request.TemplateId)
                .Select(d => (int?)d.Order).MaxAsync(ct) ?? 0;
            await db.CurriculumPlanDays.AddAsync(
                new CurriculumPlanDay(request.TemplateId, maxOrder + 1, request.Title), ct);
            await db.SaveChangesAsync(ct);
            return await BuildPlanAsync(request.TemplateId, ct);
        }, ct);
    }

    public async Task<Result<TemplatePlanDto>> Handle(RenamePlanDayCommand request, CancellationToken ct)
    {
        var day = await db.CurriculumPlanDays.FirstOrDefaultAsync(d => d.Id == request.PlanDayId, ct);
        if (day is null) return Result<TemplatePlanDto>.Fail("NOT_FOUND", "Plan day not found.");

        return await db.ExecuteInTransactionAsync(async () =>
        {
            day.Rename(request.Title);
            await db.SaveChangesAsync(ct);
            return await BuildPlanAsync(day.TemplateId, ct);
        }, ct);
    }

    public async Task<Result<TemplatePlanDto>> Handle(DeletePlanDayCommand request, CancellationToken ct)
    {
        var day = await db.CurriculumPlanDays.FirstOrDefaultAsync(d => d.Id == request.PlanDayId, ct);
        if (day is null) return Result<TemplatePlanDto>.Fail("NOT_FOUND", "Plan day not found.");
        var templateId = day.TemplateId;

        return await db.ExecuteInTransactionAsync(async () =>
        {
            var links = await db.CurriculumPlanDayLessons
                .Where(l => l.PlanDayId == request.PlanDayId).ToListAsync(ct);
            db.CurriculumPlanDayLessons.RemoveRange(links);
            db.CurriculumPlanDays.Remove(day);
            await db.SaveChangesAsync(ct);

            // Re-sequence the remaining days so orders stay contiguous (1..N).
            var remaining = await db.CurriculumPlanDays
                .Where(d => d.TemplateId == templateId).OrderBy(d => d.Order).ToListAsync(ct);
            Resequence(remaining);
            await db.SaveChangesAsync(ct);
            return await BuildPlanAsync(templateId, ct);
        }, ct);
    }

    public async Task<Result<TemplatePlanDto>> Handle(ReorderPlanDaysCommand request, CancellationToken ct)
    {
        if (!await db.CurriculumTemplates.AnyAsync(t => t.Id == request.TemplateId, ct))
            return Result<TemplatePlanDto>.Fail("NOT_FOUND", "Template not found.");

        return await db.ExecuteInTransactionAsync(async () =>
        {
            var days = await db.CurriculumPlanDays
                .Where(d => d.TemplateId == request.TemplateId).ToListAsync(ct);
            var byId = days.ToDictionary(d => d.Id);

            // Apply the requested order first, then append any days the client
            // didn't mention (defensive — keeps every day sequenced 1..N).
            var order = 1;
            foreach (var id in request.DayIds)
                if (byId.TryGetValue(id, out var d)) d.SetOrder(order++);
            foreach (var d in days.Where(d => !request.DayIds.Contains(d.Id)).OrderBy(d => d.Order))
                d.SetOrder(order++);

            await db.SaveChangesAsync(ct);
            return await BuildPlanAsync(request.TemplateId, ct);
        }, ct);
    }

    public async Task<Result<TemplatePlanDto>> Handle(SetPlanDayLessonsCommand request, CancellationToken ct)
    {
        var day = await db.CurriculumPlanDays.FirstOrDefaultAsync(d => d.Id == request.PlanDayId, ct);
        if (day is null) return Result<TemplatePlanDto>.Fail("NOT_FOUND", "Plan day not found.");
        var templateId = day.TemplateId;

        // Only lessons that actually belong to this template may be placed on its
        // days (guards against dangling links from a stale client).
        var lessonIds = request.LessonIds.Distinct().ToList();
        if (lessonIds.Count > 0)
        {
            var validIds = await (
                from l in db.CurriculumLessons
                join u in db.CurriculumUnits on l.UnitId equals u.Id
                join m in db.CurriculumModules on u.ModuleId equals m.Id
                where m.TemplateId == templateId && lessonIds.Contains(l.Id)
                select l.Id).ToListAsync(ct);
            if (validIds.Count != lessonIds.Count)
                return Result<TemplatePlanDto>.Fail("VALIDATION", "One or more lessons don't belong to this template.");
        }

        return await db.ExecuteInTransactionAsync(async () =>
        {
            var existing = await db.CurriculumPlanDayLessons
                .Where(l => l.PlanDayId == request.PlanDayId).ToListAsync(ct);
            db.CurriculumPlanDayLessons.RemoveRange(existing);

            var order = 1;
            foreach (var lessonId in lessonIds)
                await db.CurriculumPlanDayLessons.AddAsync(
                    new CurriculumPlanDayLesson(request.PlanDayId, lessonId, order++), ct);

            await db.SaveChangesAsync(ct);
            return await BuildPlanAsync(templateId, ct);
        }, ct);
    }

    private static void Resequence(IReadOnlyList<CurriculumPlanDay> ordered)
    {
        for (var i = 0; i < ordered.Count; i++)
            if (ordered[i].Order != i + 1) ordered[i].SetOrder(i + 1);
    }

    /// <summary>Rebuilds the full plan DTO (mirrors <see cref="GetTemplatePlanHandler"/>).</summary>
    private async Task<Result<TemplatePlanDto>> BuildPlanAsync(Guid templateId, CancellationToken ct)
    {
        var t = await db.CurriculumTemplates.AsNoTracking()
            .Where(x => x.Id == templateId)
            .Select(x => new { x.Id, x.Name }).FirstOrDefaultAsync(ct);
        if (t is null) return Result<TemplatePlanDto>.Fail("NOT_FOUND", "Template not found.");

        var days = await db.CurriculumPlanDays.AsNoTracking()
            .Where(d => d.TemplateId == templateId).OrderBy(d => d.Order)
            .Select(d => new { d.Id, d.Order, d.Title }).ToListAsync(ct);
        var dayIds = days.Select(d => d.Id).ToList();

        var dayLessons = await (
            from pl in db.CurriculumPlanDayLessons.AsNoTracking()
            join l in db.CurriculumLessons.AsNoTracking() on pl.CurriculumLessonId equals l.Id
            where dayIds.Contains(pl.PlanDayId)
            orderby pl.Order
            select new
            {
                pl.PlanDayId, pl.Order,
                l.Id, l.Title, l.LessonType, l.IsAssessment, l.XpReward,
            }).ToListAsync(ct);

        var dto = new TemplatePlanDto(t.Id, t.Name, days.Count,
            days.Select(d => new TemplatePlanDayDto(d.Id, d.Order, d.Title,
                dayLessons.Where(x => x.PlanDayId == d.Id)
                    .Select(x => new TemplatePlanDayLessonDto(
                        x.Id, x.Order, x.Title, x.LessonType, x.IsAssessment, x.XpReward))
                    .ToList())).ToList());

        return Result<TemplatePlanDto>.Ok(dto);
    }
}
