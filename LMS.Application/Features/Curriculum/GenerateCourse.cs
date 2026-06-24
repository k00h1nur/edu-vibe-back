using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Features.Sessions;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Curriculum;

/// <summary>
/// F3 one-shot course setup: clone a template onto the class (once), generate the
/// session calendar (2–3 lessons/day via <see cref="ScheduleSlot"/>), and map the
/// upcoming sessions to the cloned lessons in curriculum order — all inside ONE
/// transaction so a mid-flow failure leaves the class entirely unconfigured rather
/// than half-set-up. Idempotent: a re-run with the same template reuses the
/// existing clone and re-applies the (already idempotent) schedule + mapping, so
/// it never stacks duplicate courses.
/// </summary>
public sealed record GenerateCourseCommand(
    Guid ClassId,
    Guid TemplateId,
    SchedulePatternType Type,
    int DaysOfWeekMask,
    DateOnly StartDate,
    DateOnly EndDate,
    IReadOnlyList<ScheduleSlot> Slots,
    Guid? RoomId) : IRequest<Result<GenerateCourseResultDto>>;

public sealed record GenerateCourseResultDto(
    Guid ClassId,
    Guid CurriculumTemplateId,
    int GeneratedSessions,
    int RemovedSessions,
    int MappedLessons);

public sealed class GenerateCourseHandler(IApplicationDbContext db, ISender sender)
    : IRequestHandler<GenerateCourseCommand, Result<GenerateCourseResultDto>>
{
    public async Task<Result<GenerateCourseResultDto>> Handle(GenerateCourseCommand request, CancellationToken ct)
    {
        if (request.Slots is null || request.Slots.Count == 0)
            return Result<GenerateCourseResultDto>.Fail("VALIDATION", "At least one daily time slot is required.");

        var cls = await db.Classes.FirstOrDefaultAsync(c => c.Id == request.ClassId, ct);
        if (cls is null) return Result<GenerateCourseResultDto>.Fail("NOT_FOUND", "Class not found.");

        var template = await db.CurriculumTemplates.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, ct);
        if (template is null) return Result<GenerateCourseResultDto>.Fail("NOT_FOUND", "Template not found.");

        var primary = request.Slots.OrderBy(s => s.StartsAt).First();
        var expectedCloneName = $"{cls.Title} — {template.Name}";

        // One transaction across all three steps. We return early (without
        // committing) on any sub-failure; `await using` then disposes the
        // transaction and rolls everything back — no double-save, no swallowed
        // errors, no half-configured class.
        await using var tx = await db.BeginTransactionAsync(ct);

        // 1. Clone — unless this class already holds the clone of THIS template
        //    (idempotent re-run). A clone of a different template, or a directly
        //    assigned system template, is superseded by a fresh clone.
        Guid? reuseId = null;
        if (cls.CurriculumTemplateId is { } currentId)
        {
            var current = await db.CurriculumTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == currentId, ct);
            if (current is { IsSystem: false } && current.Name == expectedCloneName)
                reuseId = currentId;
        }

        Guid courseTemplateId;
        if (reuseId is { } rid)
        {
            courseTemplateId = rid;
        }
        else
        {
            var clone = await sender.Send(new CloneTemplateToClassCommand(request.ClassId, request.TemplateId), ct);
            if (!clone.Success)
                return Result<GenerateCourseResultDto>.Fail(clone.ErrorCode ?? "FAILED", clone.Message ?? "Clone failed.");
            // The clone set Class.CurriculumTemplateId on the shared tracked entity.
            courseTemplateId = cls.CurriculumTemplateId
                ?? throw new InvalidOperationException("Clone did not set the class curriculum template.");
        }

        // 2. Generate the session calendar (N-per-day via Slots).
        var sched = await sender.Send(new ApplyClassScheduleCommand(
            request.ClassId, request.Type, request.DaysOfWeekMask,
            request.StartDate, request.EndDate, primary.StartsAt, primary.EndsAt,
            request.RoomId, request.Slots), ct);
        if (!sched.Success || sched.Data is null)
            return Result<GenerateCourseResultDto>.Fail(sched.ErrorCode ?? "FAILED", sched.Message ?? "Scheduling failed.");

        // 3. Map upcoming sessions to the cloned course's lessons in order.
        var assign = await sender.Send(new AssignCurriculumToClassCommand(request.ClassId, courseTemplateId), ct);
        if (!assign.Success)
            return Result<GenerateCourseResultDto>.Fail(assign.ErrorCode ?? "FAILED", assign.Message ?? "Curriculum mapping failed.");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var mapped = await db.ClassSessions.AsNoTracking()
            .CountAsync(s => s.ClassId == request.ClassId && s.SessionDate >= today && s.CurriculumLessonId != null, ct);

        await tx.CommitAsync(ct);

        return Result<GenerateCourseResultDto>.Ok(
            new GenerateCourseResultDto(request.ClassId, courseTemplateId,
                sched.Data.GeneratedCount, sched.Data.RemovedCount, mapped),
            $"Course ready: {sched.Data.GeneratedCount} session(s), {mapped} mapped to lessons.");
    }
}
