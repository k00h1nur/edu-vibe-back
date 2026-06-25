using LMS.Application.Common;
using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
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
        // SECURITY: ignore the StudentProfileId on the wire — always force the
        // submission onto the caller's own student profile. Without this, a
        // student could pass any other student's id in the body and submit
        // work as them. The Submit permission is only granted to the Student
        // role, so anyone reaching this handler must have a student profile.
        var callerStudentProfileId = currentUser.StudentProfileId;
        if (callerStudentProfileId is null)
            return Result<TaskSubmissionDto>.Fail("FORBIDDEN", "Only students may submit responses.");

        var task = await db.LearningTasks
            .FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken);
        if (task is null) return Result<TaskSubmissionDto>.Fail("NOT_FOUND", "Task not found.");

        // F3↔F4 date-gate (shared rule): a student can't submit before the lesson day
        // (school-local). Closes the direct-endpoint bypass — list/read/submit all gate.
        var lessonDate = await db.Assignments.Where(a => a.Id == task.AssignmentId)
            .Select(a => a.ClassSessionId == null
                ? (DateOnly?)null
                : db.ClassSessions.Where(s => s.Id == a.ClassSessionId)
                    .Select(s => (DateOnly?)s.SessionDate).FirstOrDefault())
            .FirstOrDefaultAsync(cancellationToken);
        if (!SchoolCalendar.IsLessonHomeworkVisibleToStudent(lessonDate, SchoolCalendar.Today(DateTime.UtcNow)))
            return Result<TaskSubmissionDto>.Fail("FORBIDDEN", "This lesson's homework isn't open yet.");

        // Allow re-submission by updating the existing row (one submission per
        // (task, student)).
        var submission = await db.TaskSubmissions.FirstOrDefaultAsync(
            s => s.TaskId == request.TaskId && s.StudentProfileId == callerStudentProfileId,
            cancellationToken);

        if (submission is null)
        {
            submission = new TaskSubmission(request.TaskId, callerStudentProfileId.Value, request.ResponseJson);
            await db.TaskSubmissions.AddAsync(submission, cancellationToken);
        }
        else
        {
            submission.UpdateResponse(request.ResponseJson);
        }

        var verdict = grader.Grade(task, request.ResponseJson);
        if (verdict.AutoGraded)
        {
            submission.Grade(verdict.Score, verdict.IsCorrect, gradedByUserId: null, feedback: null);
            await AwardXpIfFirstGradeAsync(submission, task, cancellationToken);
        }
        else
        {
            submission.AwaitManualGrading();
        }

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

        var task = await db.LearningTasks.FirstOrDefaultAsync(t => t.Id == submission.TaskId, cancellationToken);
        if (task is not null)
            await AwardXpIfFirstGradeAsync(submission, task, cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
        return Result<TaskSubmissionDto>.Ok(Map(submission));
    }

    public async Task<Result<IReadOnlyCollection<TaskSubmissionDto>>> Handle(
        GetTaskSubmissionsByTaskQuery request, CancellationToken cancellationToken)
    {
        // SECURITY: this returns EVERY student's submission for the task (the
        // teacher grading view). Staff only — a student must use the self-scoped
        // GetMyTaskSubmissionsByAssignmentQuery, or they could read peers' answers.
        if (currentUser.StaffProfileId is null)
            return Result<IReadOnlyCollection<TaskSubmissionDto>>.Fail(
                "FORBIDDEN", "Only staff may read all submissions for a task.");

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
        // SECURITY: students see their own submissions only. Staff (anyone with
        // a StaffProfileId in the JWT — teachers, support teachers, admins,
        // office) can read any student's submissions for grading workflows.
        var targetStudentProfileId = request.StudentProfileId;
        if (currentUser.StaffProfileId is null)
        {
            if (currentUser.StudentProfileId is null)
                return Result<IReadOnlyCollection<TaskSubmissionDto>>.Fail(
                    "FORBIDDEN", "Caller is neither a student nor staff.");
            if (currentUser.StudentProfileId != targetStudentProfileId)
                return Result<IReadOnlyCollection<TaskSubmissionDto>>.Fail(
                    "FORBIDDEN", "Students may only read their own submissions.");
        }

        var items = await db.TaskSubmissions
            .Where(s => s.StudentProfileId == targetStudentProfileId
                        && s.Task != null && s.Task.AssignmentId == request.AssignmentId)
            .OrderBy(s => s.Task!.Order)
            .Select(s => new TaskSubmissionDto(
                s.Id, s.TaskId, s.StudentProfileId, s.ResponseJson,
                s.Score, s.IsCorrect, s.Status, s.TeacherFeedback,
                s.CreatedAt, s.GradedAt))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<TaskSubmissionDto>>.Ok(items);
    }

    /// <summary>
    /// Grants XP for a freshly graded submission — ONCE. The single award path for
    /// both auto-grade (submit) and manual grade. XP = round(points × score);
    /// skipped cleanly when already awarded or when it rounds to 0 (AddXp throws on
    /// ≤ 0). The AddXp + ledger row + XpAwarded flag all flush in the caller's one
    /// SaveChanges, so they can never drift apart.
    /// </summary>
    private async Task AwardXpIfFirstGradeAsync(TaskSubmission submission, LearningTask task, CancellationToken ct)
    {
        if (submission.XpAwarded || submission.Score is not { } score) return;
        var xp = TaskXp.ForGrade(task.Points, score);
        if (xp <= 0) return;

        var sp = await db.StudentProfiles.FirstOrDefaultAsync(p => p.Id == submission.StudentProfileId, ct);
        if (sp is null) return;

        sp.AddXp(xp);
        await db.XpLedger.AddAsync(
            XpLedger.CreateEntry(sp.Id, xp, XpSourceType.TaskGrading, $"Task:{task.Title}"), ct);
        submission.MarkXpAwarded();
    }

    private static TaskSubmissionDto Map(TaskSubmission s) => new(
        s.Id, s.TaskId, s.StudentProfileId, s.ResponseJson,
        s.Score, s.IsCorrect, s.Status, s.TeacherFeedback,
        s.CreatedAt, s.GradedAt);
}
