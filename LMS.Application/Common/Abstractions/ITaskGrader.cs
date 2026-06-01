using LMS.Domain.Entities;

namespace LMS.Application.Common.Abstractions;

/// <summary>
/// Auto-grades a <see cref="TaskSubmission"/> against the parent
/// <see cref="LearningTask"/>'s solution. Closed-form types (multiple choice,
/// fill-gaps, matching, ordering, short answer with exact match) get a
/// definitive verdict. Open-ended types (listening short-answer with free
/// text, the composite Test) return a <c>RequiresManualReview</c> result
/// instead so a teacher resolves them.
/// </summary>
public interface ITaskGrader
{
    GradeResult Grade(LearningTask task, string responseJson);
}

public readonly record struct GradeResult(
    bool AutoGraded,
    decimal Score,
    bool IsCorrect)
{
    public static GradeResult RequiresManualReview { get; } = new(false, 0m, false);
    public static GradeResult Correct { get; } = new(true, 1m, true);
    public static GradeResult Incorrect { get; } = new(true, 0m, false);
    public static GradeResult Partial(decimal score) => new(true, score, score >= 1m);
}
