using LMS.Application.Common.Abstractions;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Tasks;

public sealed record MaterializeOutcome(Guid? AssignmentId, int CreatedTasks, int TotalBlueprints, bool HadBlueprints);

/// <summary>
/// Shared, fail-soft, idempotent core that turns a lesson's <see cref="LessonDefaultTask"/>
/// blueprints into real <see cref="LearningTask"/>s under the session's assignment.
/// Both the manual endpoint and GenerateCourse's auto-materialize call THIS — one code
/// path, so they can't drift. NO authorization here (callers authorize); NO transaction
/// management (uses the ambient context, so GenerateCourse's transaction wraps it).
/// Fail-soft: a missing session / unlinked lesson / no blueprints returns 0, never throws.
/// </summary>
public interface ILessonTaskMaterializer
{
    Task<MaterializeOutcome> MaterializeAsync(Guid classSessionId, Guid createdByUserId, CancellationToken ct);
}

public sealed class LessonTaskMaterializer(IApplicationDbContext db) : ILessonTaskMaterializer
{
    public async Task<MaterializeOutcome> MaterializeAsync(Guid classSessionId, Guid createdByUserId, CancellationToken ct)
    {
        var session = await db.ClassSessions.FirstOrDefaultAsync(s => s.Id == classSessionId, ct);
        if (session is null || session.CurriculumLessonId is not { } lessonId)
            return new MaterializeOutcome(null, 0, 0, false);

        var blueprints = await db.LessonDefaultTasks.AsNoTracking()
            .Where(t => t.CurriculumLessonId == lessonId)
            .OrderBy(t => t.Order)
            .ToListAsync(ct);
        if (blueprints.Count == 0)
            return new MaterializeOutcome(null, 0, 0, false);

        // Reuse the session's assignment if present (idempotent); else create one.
        var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.ClassSessionId == session.Id, ct);
        if (assignment is null)
        {
            var creator = await db.Users.FirstOrDefaultAsync(u => u.Id == createdByUserId, ct);
            if (creator is null) return new MaterializeOutcome(null, 0, blueprints.Count, true);
            var title = string.IsNullOrWhiteSpace(session.Topic) ? "Lesson tasks" : session.Topic!;
            assignment = new Assignment(session.ClassId, title, creator);
            assignment.SetSession(session.Id);
            await db.Assignments.AddAsync(assignment, ct);
            await db.SaveChangesAsync(ct);
        }

        var existingOrders = (await db.LearningTasks.AsNoTracking()
                .Where(t => t.AssignmentId == assignment.Id).Select(t => t.Order).ToListAsync(ct))
            .ToHashSet();

        // Pure idempotency core decides what to add — re-runs add nothing.
        var ordersToCreate = LessonTaskMaterialization
            .OrdersToCreate(blueprints.Select(b => b.Order), existingOrders)
            .ToHashSet();

        var created = 0;
        foreach (var bp in blueprints)
        {
            if (!ordersToCreate.Contains(bp.Order)) continue;
            await db.LearningTasks.AddAsync(
                new LearningTask(assignment.Id, bp.Order, bp.Type, bp.Title, bp.Points, bp.ContentJson, bp.SolutionJson), ct);
            created++;
        }
        if (created > 0) await db.SaveChangesAsync(ct);

        return new MaterializeOutcome(assignment.Id, created, blueprints.Count, true);
    }
}
