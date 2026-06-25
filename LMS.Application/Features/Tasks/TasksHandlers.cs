using LMS.Application.Common;
using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Tasks;

public sealed class TasksHandlers(IApplicationDbContext db, ICurrentUserService currentUser) :
    IRequestHandler<GetAssignmentTasksQuery, Result<IReadOnlyCollection<LearningTaskDto>>>,
    IRequestHandler<GetTaskByIdQuery, Result<LearningTaskDto>>,
    IRequestHandler<CreateTaskCommand, Result<LearningTaskDto>>,
    IRequestHandler<UpdateTaskCommand, Result<LearningTaskDto>>,
    IRequestHandler<DeleteTaskCommand, Result>,
    IRequestHandler<ReorderTasksCommand, Result>
{
    /// <summary>
    /// F3↔F4 date-gate (shared rule): a STUDENT may only read a lesson's tasks on/after
    /// the lesson day (school-local). Staff are never gated. Assignments with no dated
    /// lesson are always visible. Enrollment scoping is handled elsewhere — this is the date lock.
    /// </summary>
    private async Task<bool> IsAssignmentDateVisibleToCallerAsync(Guid assignmentId, CancellationToken ct)
    {
        if (currentUser.StaffProfileId is not null || currentUser.StudentProfileId is null) return true;
        var lessonDate = await db.Assignments.Where(a => a.Id == assignmentId)
            .Select(a => a.ClassSessionId == null
                ? (DateOnly?)null
                : db.ClassSessions.Where(s => s.Id == a.ClassSessionId)
                    .Select(s => (DateOnly?)s.SessionDate).FirstOrDefault())
            .FirstOrDefaultAsync(ct);
        return SchoolCalendar.IsLessonHomeworkVisibleToStudent(lessonDate, SchoolCalendar.Today(DateTime.UtcNow));
    }

    public async Task<Result<IReadOnlyCollection<LearningTaskDto>>> Handle(
        GetAssignmentTasksQuery request, CancellationToken cancellationToken)
    {
        if (!await IsAssignmentDateVisibleToCallerAsync(request.AssignmentId, cancellationToken))
            return Result<IReadOnlyCollection<LearningTaskDto>>.Fail(
                "FORBIDDEN", "This lesson's homework unlocks on the lesson day.");

        var items = await db.LearningTasks
            .Where(t => t.AssignmentId == request.AssignmentId)
            .OrderBy(t => t.Order)
            .Select(t => new LearningTaskDto(
                t.Id, t.AssignmentId, t.Order, t.Type, t.Title, t.Points,
                t.ContentJson,
                request.IncludeSolutions ? t.SolutionJson : null,
                t.CreatedAt))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<LearningTaskDto>>.Ok(items);
    }

    public async Task<Result<LearningTaskDto>> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken)
    {
        var t = await db.LearningTasks.FirstOrDefaultAsync(x => x.Id == request.TaskId, cancellationToken);
        if (t is null) return Result<LearningTaskDto>.Fail("NOT_FOUND", "Task not found.");
        if (!await IsAssignmentDateVisibleToCallerAsync(t.AssignmentId, cancellationToken))
            return Result<LearningTaskDto>.Fail("FORBIDDEN", "This lesson's homework unlocks on the lesson day.");
        return Result<LearningTaskDto>.Ok(new LearningTaskDto(
            t.Id, t.AssignmentId, t.Order, t.Type, t.Title, t.Points,
            t.ContentJson,
            request.IncludeSolution ? t.SolutionJson : null,
            t.CreatedAt));
    }

    public async Task<Result<LearningTaskDto>> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var assignmentExists = await db.Assignments
            .AnyAsync(a => a.Id == request.AssignmentId, cancellationToken);
        if (!assignmentExists) return Result<LearningTaskDto>.Fail("NOT_FOUND", "Assignment not found.");

        var task = new LearningTask(
            request.AssignmentId, request.Order, request.Type, request.Title,
            request.Points, request.ContentJson, request.SolutionJson);
        await db.LearningTasks.AddAsync(task, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<LearningTaskDto>.Ok(Map(task, includeSolution: true));
    }

    public async Task<Result<LearningTaskDto>> Handle(UpdateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await db.LearningTasks.FirstOrDefaultAsync(x => x.Id == request.TaskId, cancellationToken);
        if (task is null) return Result<LearningTaskDto>.Fail("NOT_FOUND", "Task not found.");

        task.Update(request.Order, request.Title, request.Points, request.ContentJson, request.SolutionJson);
        await db.SaveChangesAsync(cancellationToken);
        return Result<LearningTaskDto>.Ok(Map(task, includeSolution: true));
    }

    public async Task<Result> Handle(DeleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await db.LearningTasks.FirstOrDefaultAsync(x => x.Id == request.TaskId, cancellationToken);
        if (task is null) return Result.Fail("NOT_FOUND", "Task not found.");

        // Cascading via FK removes submissions; explicit Remove keeps the path obvious.
        db.LearningTasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Task deleted.");
    }

    public async Task<Result> Handle(ReorderTasksCommand request, CancellationToken cancellationToken)
    {
        var tasks = await db.LearningTasks
            .Where(t => t.AssignmentId == request.AssignmentId)
            .ToListAsync(cancellationToken);
        var byId = tasks.ToDictionary(t => t.Id);

        for (var i = 0; i < request.TaskIdsInOrder.Count; i++)
        {
            if (byId.TryGetValue(request.TaskIdsInOrder[i], out var t))
            {
                t.Update(i, t.Title, t.Points, t.ContentJson, t.SolutionJson);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Tasks reordered.");
    }

    internal static LearningTaskDto Map(LearningTask t, bool includeSolution) => new(
        t.Id, t.AssignmentId, t.Order, t.Type, t.Title, t.Points,
        t.ContentJson, includeSolution ? t.SolutionJson : null, t.CreatedAt);
}
