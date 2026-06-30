using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A user's self-check attempt at a <see cref="LessonExercise"/>. Exactly ONE row per
/// (exercise, user) — re-submitting overwrites it (upsert). <see cref="AnswersJson"/>
/// mirrors the exercise's answer shape; <see cref="Score"/> / <see cref="Total"/> come
/// from ExerciseChecker. Self-check only — no XP, no teacher grading.
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

    /// <summary>Record (or re-record) the user's answers + checked result. Completing = attempting.</summary>
    public void Apply(string answersJson, int score, int total)
    {
        AnswersJson = string.IsNullOrWhiteSpace(answersJson) ? "{}" : answersJson;
        Score = score;
        Total = total;
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        Touch();
    }
}
