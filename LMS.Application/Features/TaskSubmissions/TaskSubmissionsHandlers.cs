using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.TaskSubmissions;

public sealed class TaskSubmissionsHandlers(
    IApplicationDbContext db,
    ITaskGrader grader,
    ICurrentUserService currentUser)
    : IRequestHandler<SubmitTaskResponseCommand, Result<TaskSubmissionDto>>,
      IRequestHandler<GradeTaskSubmissionCommand, Result<TaskSubmissionDto>>,
      IRequestHandler<GetTaskSubmissionsByTaskQuery, Result<IReadOnlyCollection<TaskSubmissionDto>>>,
      IRequestHandler<GetMyTaskSubmissionsByAssignmentQuery, Result<IReadOnlyCollection<TaskSubmissionDto>>>
{
    public async Task<Result<TaskSubmissionDto>> Handle(
        SubmitTaskResponseCommand request, CancellationToken cancellationToken)
    {
        var task = await db.LearningTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);
        if (task is null) return Result<TaskSubmissionDto>.Fail("NOT_FOUND", "Task not found.");

        // Allow re-submission by updating the existing row (one submission per
        // (task, student)).
        var submission = await db.TaskSubmissions.FirstOrDefaultAsync(
            s => s.TaskId == request.TaskId && s.StudentProfileId == request.StudentProfileId,
            cancellationToken);

        if (submission is null)
        {
            submission = new TaskSubmission(request.TaskId, request.StudentProfileId, request.ResponseJson);
            await db.TaskSubmissions.AddAsync(submission, cancellationToken);
        }
        else
        {
            submission.UpdateResponse(request.ResponseJson);
        }

        var verdict = grader.Grade(task, request.ResponseJson);
        if (verdict.AutoGraded)
            submission.Grade(verdict.Score, verdict.IsCorrect, gradedByUserId: null, feedback: null);
        else
            submission.AwaitManualGrading();

        await db.SaveChangesAsync(cancellationToken);
        return Result<TaskSubmissionDto>.Ok(Map(submission), "Submission recorded.");
    }

    public async Task<Result<TaskSubmissionDto>> Handle(
        GradeTaskSubmissionCommand request, CancellationToken cancellationToken)
    {
        var submission = await db.TaskSubmissions
            .FirstOrDefaultAsync(s => s.Id == request.SubmissionId, cancellationToken);
        if (submission is null) return Result<TaskSubmissionDto>.Fail("NOT_FOUND", "Submission not found.");

        var isCorrect = request.Score >= 1m;
        submission.Grade(request.Score, isCorrect, currentUser.UserId, request.Feedback);
        await db.SaveChangesAsync(cancellationToken);
        return Result<TaskSubmissionDto>.Ok(Map(submission));
    }

    public async Task<Result<IReadOnlyCollection<TaskSubmissionDto>>> Handle(
        GetTaskSubmissionsByTaskQuery request, CancellationToken cancellationToken)
    {
        var items = await db.TaskSubmissions
            .Where(s => s.TaskId == request.TaskId)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new TaskSubmissionDto(
                s.Id, s.TaskId, s.StudentProfileId, s.ResponseJson,
                s.Score, s.IsCorrect, s.Status, s.TeacherFeedback,
                s.CreatedAt, s.GradedAt))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<TaskSubmissionDto>>.Ok(items);
    }

    public async Task<Result<IReadOnlyCollection<TaskSubmissionDto>>> Handle(
        GetMyTaskSubmissionsByAssignmentQuery request, CancellationToken cancellationToken)
    {
        var items = await db.TaskSubmissions
            .Where(s => s.StudentProfileId == request.StudentProfileId
                        && s.Task != null && s.Task.AssignmentId == request.AssignmentId)
            .OrderBy(s => s.Task!.Order)
            .Select(s => new TaskSubmissionDto(
                s.Id, s.TaskId, s.StudentProfileId, s.ResponseJson,
                s.Score, s.IsCorrect, s.Status, s.TeacherFeedback,
                s.CreatedAt, s.GradedAt))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<TaskSubmissionDto>>.Ok(items);
    }

    private static TaskSubmissionDto Map(TaskSubmission s) => new(
        s.Id, s.TaskId, s.StudentProfileId, s.ResponseJson,
        s.Score, s.IsCorrect, s.Status, s.TeacherFeedback,
        s.CreatedAt, s.GradedAt);
}
