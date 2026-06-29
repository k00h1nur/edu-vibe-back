using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Curriculum;

// ---- Template teaching-plan read (the day-by-day view: Day 1 = 1A + 1B …) -----

/// <summary>One curriculum lesson on a plan-day, with its in-day order (1A=1, 1B=2).</summary>
public sealed record TemplatePlanDayLessonDto(
    Guid LessonId, int Order, string Title, CurriculumLessonType LessonType, bool IsAssessment, int XpReward);

/// <summary>A "class day" in the plan — covers 1+ lessons (paired) or a single exam.</summary>
public sealed record TemplatePlanDayDto(
    Guid Id, int Order, string? Title, IReadOnlyList<TemplatePlanDayLessonDto> Lessons);

/// <summary>A template's full reusable teaching plan, ordered by day.</summary>
public sealed record TemplatePlanDto(
    Guid TemplateId, string TemplateName, int TotalDays, IReadOnlyList<TemplatePlanDayDto> Days);

public sealed record GetTemplatePlanQuery(Guid TemplateId) : IRequest<Result<TemplatePlanDto>>;

/// <summary>
/// Returns the template's reusable day-plan (curriculum_plan_days +
/// curriculum_plan_day_lessons) for the day-card view. A template with no plan loaded
/// returns an empty Days list (valid, not an error). Read-only.
/// </summary>
public sealed class GetTemplatePlanHandler(IApplicationDbContext db)
    : IRequestHandler<GetTemplatePlanQuery, Result<TemplatePlanDto>>
{
    public async Task<Result<TemplatePlanDto>> Handle(GetTemplatePlanQuery request, CancellationToken ct)
    {
        var t = await db.CurriculumTemplates.AsNoTracking()
            .Where(x => x.Id == request.TemplateId)
            .Select(x => new { x.Id, x.Name }).FirstOrDefaultAsync(ct);
        if (t is null) return Result<TemplatePlanDto>.Fail("NOT_FOUND", "Template not found.");

        var days = await db.CurriculumPlanDays.AsNoTracking()
            .Where(d => d.TemplateId == request.TemplateId).OrderBy(d => d.Order)
            .Select(d => new { d.Id, d.Order, d.Title }).ToListAsync(ct);
        var dayIds = days.Select(d => d.Id).ToList();

        // Lessons per day (kept flat with PlanDayId so we group in memory — avoids the
        // multi-collection Include the DbContext rejects).
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
