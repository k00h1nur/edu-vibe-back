using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A student's response to a single <see cref="LearningTask"/>. The shape of
/// <see cref="ResponseJson"/> mirrors what the corresponding task expects (see
/// <see cref="LearningTask"/> docs). For auto-gradable types the handler runs
/// <c>ITaskGrader.Grade</c> on submit and stores the result; manually-graded
/// types stay at <see cref="TaskSubmissionStatus.Submitted"/> until a teacher
/// reviews.
/// </summary>
public sealed class TaskSubmission : BaseEntity
{
    private TaskSubmission() { }

    public TaskSubmission(Guid taskId, Guid studentProfileId, string responseJson)
    {
        if (taskId == Guid.Empty) throw new DomainException("Task id is required.");
        if (studentProfileId == Guid.Empty) throw new DomainException("Student profile id is required.");
        if (string.IsNullOrWhiteSpace(responseJson)) throw new DomainException("Response is required.");
        TaskId = taskId;
        StudentProfileId = studentProfileId;
        ResponseJson = responseJson;
        Status = TaskSubmissionStatus.Submitted;
    }

    public Guid TaskId { get; private set; }
    public LearningTask? Task { get; private set; }

    public Guid StudentProfileId { get; private set; }
    public StudentProfile? StudentProfile { get; private set; }

    public string ResponseJson { get; private set; } = "{}";

    /// <summary>0.0 = fully wrong, 1.0 = fully correct. Null until graded.</summary>
    public decimal? Score { get; private set; }
    public bool? IsCorrect { get; private set; }
    public TaskSubmissionStatus Status { get; private set; }
    public DateTime? GradedAt { get; private set; }
    public Guid? GradedByUserId { get; private set; }
    public string? TeacherFeedback { get; private set; }

    /// <summary>
    /// True once XP has been granted for this submission (F4). Award-once:
    /// deliberately NOT reset by <see cref="UpdateResponse"/>, so re-submitting or
    /// re-grading can never grant XP twice. No claw-back or top-up on re-grade.
    /// </summary>
    public bool XpAwarded { get; private set; }

    /// <summary>Apply a grader's verdict (auto or manual).</summary>
    public void Grade(decimal score, bool isCorrect, Guid? gradedByUserId, string? feedback)
    {
        if (score < 0m || score > 1m) throw new DomainException("Score must be between 0 and 1.");
        Score = score;
        IsCorrect = isCorrect;
        Status = TaskSubmissionStatus.Graded;
        GradedAt = DateTime.UtcNow;
        GradedByUserId = gradedByUserId;
        TeacherFeedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        Touch();
    }

    /// <summary>Flag that XP has been granted for this submission (idempotency guard).</summary>
    public void MarkXpAwarded()
    {
        XpAwarded = true;
        Touch();
    }

    /// <summary>Mark as awaiting manual review by a teacher.</summary>
    public void AwaitManualGrading()
    {
        Status = TaskSubmissionStatus.AwaitingReview;
        Touch();
    }

    /// <summary>Update the response (re-submit). Resets grading state.</summary>
    public void UpdateResponse(string responseJson)
    {
        if (string.IsNullOrWhiteSpace(responseJson)) throw new DomainException("Response is required.");
        ResponseJson = responseJson;
        Status = TaskSubmissionStatus.Submitted;
        Score = null;
        IsCorrect = null;
        GradedAt = null;
        GradedByUserId = null;
        TeacherFeedback = null;
        Touch();
    }
}

public enum TaskSubmissionStatus
{
    Submitted = 1,
    AwaitingReview = 2,
    Graded = 3,
}
