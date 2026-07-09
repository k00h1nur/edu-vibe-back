using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A user's self-check attempt at a <see cref="LessonExercise"/>. Exactly ONE row per
/// (exercise, user) — re-submitting overwrites it (upsert). <see cref="AnswersJson"/>
/// mirrors the exercise's answer shape; <see cref="Score"/> / <see cref="Total"/> come
/// from ExerciseChecker. Self-check only — no XP.
///
/// Open-ended types (e.g. <c>writing</c>) auto-check to 0/0, so a teacher can grade them
/// by hand: <see cref="TeacherScore"/> out of <see cref="TeacherMaxScore"/> plus optional
/// <see cref="TeacherFeedback"/>. A fresh <see cref="Apply"/> (the student edited + re-sent)
/// clears any earlier grade, since the graded text no longer exists.
/// </summary>
public sealed class LessonExerciseSubmission : BaseEntity
{
    private LessonExerciseSubmission() { }

    public LessonExerciseSubmission(Guid lessonExerciseId, Guid userId, string answersJson, int score, int total)
    {
        if (lessonExerciseId == Guid.Empty) throw new DomainException("Exercise id is required.");
        if (userId == Guid.Empty) throw new DomainException("User id is required.");
        LessonExerciseId = lessonExerciseId;
        UserId = userId;
        Apply(answersJson, score, total);
    }

    public Guid LessonExerciseId { get; private set; }
    public LessonExercise? LessonExercise { get; private set; }

    public Guid UserId { get; private set; }

    /// <summary>jsonb — the user's submitted answers (shape mirrors the exercise type).</summary>
    public string AnswersJson { get; private set; } = "{}";

    public int Score { get; private set; }
    public int Total { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    // ---- teacher grading (open-ended types like writing) --------------------

    /// <summary>Teacher-awarded score, out of <see cref="TeacherMaxScore"/>. Null ⇒ not graded.</summary>
    public decimal? TeacherScore { get; private set; }

    /// <summary>The scale the teacher graded on (e.g. 10 for "8/10"). Null ⇒ not graded.</summary>
    public decimal? TeacherMaxScore { get; private set; }

    /// <summary>Optional written feedback the student sees alongside the grade.</summary>
    public string? TeacherFeedback { get; private set; }

    public Guid? GradedByUserId { get; private set; }
    public DateTime? GradedAt { get; private set; }

    /// <summary>True once a teacher has graded this submission.</summary>
    public bool IsTeacherGraded => GradedAt is not null;

    /// <summary>Record (or re-record) the user's answers + checked result. Completing = attempting.
    /// Clears any prior teacher grade — the answers just changed, so the old grade is stale.</summary>
    public void Apply(string answersJson, int score, int total)
    {
        AnswersJson = string.IsNullOrWhiteSpace(answersJson) ? "{}" : answersJson;
        Score = score;
        Total = total;
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        TeacherScore = null;
        TeacherMaxScore = null;
        TeacherFeedback = null;
        GradedByUserId = null;
        GradedAt = null;
        Touch();
    }

    /// <summary>Teacher grades this submission: <paramref name="score"/> out of
    /// <paramref name="maxScore"/> (decimals allowed, e.g. band 6.5) plus optional feedback.</summary>
    public void Grade(decimal score, decimal maxScore, string? feedback, Guid gradedByUserId)
    {
        if (gradedByUserId == Guid.Empty) throw new DomainException("Grader id is required.");
        if (maxScore <= 0) throw new DomainException("Max score must be greater than zero.");
        if (score < 0 || score > maxScore) throw new DomainException("Score must be between 0 and the max.");
        TeacherScore = score;
        TeacherMaxScore = maxScore;
        TeacherFeedback = string.IsNullOrWhiteSpace(feedback) ? null : feedback.Trim();
        GradedByUserId = gradedByUserId;
        GradedAt = DateTime.UtcNow;
        Touch();
    }
}
