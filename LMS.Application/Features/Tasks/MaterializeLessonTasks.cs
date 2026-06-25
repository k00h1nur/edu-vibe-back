using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Tasks;

/// <summary>
/// F4: materialise a lesson's default-task blueprints (F3 <see cref="LessonDefaultTask"/>)
/// into real gradeable <see cref="LearningTask"/>s under one Assignment for the
/// session. Idempotent — reuses the session's assignment and skips blueprints
/// already materialised (by Order), so re-running "Add tasks to this lesson" never
/// creates a second assignment or duplicate tasks.
/// </summary>
public sealed record MaterializeLessonTasksCommand(Guid ClassSessionId)
    : IRequest<Result<MaterializeLessonTasksResultDto>>;

public sealed record MaterializeLessonTasksResultDto(Guid AssignmentId, int CreatedTasks, int TotalDefaultTasks);

public sealed class MaterializeLessonTasksHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<MaterializeLessonTasksCommand, Result<MaterializeLessonTasksResultDto>>
{
    public async Task<Result<MaterializeLessonTasksResultDto>> Handle(
        MaterializeLessonTasksCommand request, CancellationToken ct)
    {
        var session = await db.ClassSessions.FirstOrDefaultAsync(s => s.Id == request.ClassSessionId, ct);
        if (session is null) return Result<MaterializeLessonTasksResultDto>.Fail("NOT_FOUND", "Session not found.");

        var cls = await db.Classes.FirstOrDefaultAsync(c => c.Id == session.ClassId, ct);
        if (cls is null) return Result<MaterializeLessonTasksResultDto>.Fail("NOT_FOUND", "Class not found.");
        if (!IsAdmin && (cls.TeacherUserId is null || cls.TeacherUserId != currentUser.UserId))
            return Result<MaterializeLessonTasksResultDto>.Fail(
                "FORBIDDEN", "Only the class teacher or an admin can add tasks to this lesson.");

        if (session.CurriculumLessonId is not { } lessonId)
            return Result<MaterializeLessonTasksResultDto>.Fail(
                "VALIDATION", "This session isn't linked to a curriculum lesson.");

        var blueprints = await db.LessonDefaultTasks.AsNoTracking()
            .Where(t => t.CurriculumLessonId == lessonId)
            .OrderBy(t => t.Order)
            .ToListAsync(ct);
        if (blueprints.Count == 0)
            return Result<MaterializeLessonTasksResultDto>.Fail("VALIDATION", "This lesson has no default tasks to add.");

        // Reuse the session's assignment if present (idempotent); else create one.
        var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.ClassSessionId == session.Id, ct);
        if (assignment is null)
        {
            var teacher = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
            if (teacher is null) return Result<MaterializeLessonTasksResultDto>.Fail("FORBIDDEN", "Caller not found.");
            var title = string.IsNullOrWhiteSpace(session.Topic) ? "Lesson tasks" : session.Topic!;
            assignment = new Assignment(session.ClassId, title, teacher);
            assignment.SetSession(session.Id);
            await db.Assignments.AddAsync(assignment, ct);
            await db.SaveChangesAsync(ct);
        }

        // Existing task orders under this assignment — skip them so a re-run can't
        // duplicate a blueprint that was already materialised.
        var existingOrders = (await db.LearningTasks.AsNoTracking()
                .Where(t => t.AssignmentId == assignment.Id).Select(t => t.Order).ToListAsync(ct))
            .ToHashSet();

        var created = 0;
        foreach (var bp in blueprints)
        {
            if (!existingOrders.Add(bp.Order)) continue;
            await db.LearningTasks.AddAsync(
                new LearningTask(assignment.Id, bp.Order, bp.Type, bp.Title, bp.Points, bp.ContentJson, bp.SolutionJson), ct);
            created++;
        }
        if (created > 0) await db.SaveChangesAsync(ct);

        return Result<MaterializeLessonTasksResultDto>.Ok(
            new MaterializeLessonTasksResultDto(assignment.Id, created, blueprints.Count),
            created > 0 ? $"Added {created} task(s) to the lesson." : "Tasks already added.");
    }

    private bool IsAdmin =>
        currentUser.IsInRole(RoleCodes.Admin) || currentUser.IsInRole(RoleCodes.SuperAdmin)
        || currentUser.IsInRole(RoleCodes.OfficeAdmin);
}
