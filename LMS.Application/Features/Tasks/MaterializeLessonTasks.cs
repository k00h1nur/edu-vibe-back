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

public sealed class MaterializeLessonTasksHandler(
    IApplicationDbContext db, ICurrentUserService currentUser, ILessonTaskMaterializer materializer)
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

        if (session.CurriculumLessonId is null)
            return Result<MaterializeLessonTasksResultDto>.Fail(
                "VALIDATION", "This session isn't linked to a curriculum lesson.");

        // Delegate to the shared idempotent core (same path GenerateCourse uses).
        var outcome = await materializer.MaterializeAsync(session.Id, currentUser.UserId!.Value, ct);
        if (!outcome.HadBlueprints)
            return Result<MaterializeLessonTasksResultDto>.Fail("VALIDATION", "This lesson has no default tasks to add.");
        if (outcome.AssignmentId is not { } assignmentId)
            return Result<MaterializeLessonTasksResultDto>.Fail("FORBIDDEN", "Caller not found.");

        return Result<MaterializeLessonTasksResultDto>.Ok(
            new MaterializeLessonTasksResultDto(assignmentId, outcome.CreatedTasks, outcome.TotalBlueprints),
            outcome.CreatedTasks > 0 ? $"Added {outcome.CreatedTasks} task(s) to the lesson." : "Tasks already added.");
    }

    private bool IsAdmin =>
        currentUser.IsInRole(RoleCodes.Admin) || currentUser.IsInRole(RoleCodes.SuperAdmin)
        || currentUser.IsInRole(RoleCodes.OfficeAdmin);
}
