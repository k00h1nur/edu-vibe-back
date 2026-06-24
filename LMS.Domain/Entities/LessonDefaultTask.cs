using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A default/blueprint task attached to a curriculum lesson (F3). Cloning a
/// template onto a class deep-copies these alongside the lesson, so the class
/// owns an editable copy (Decision E: copy-at-clone — no read-only reference, no
/// override resolution). F4 will instantiate real, gradeable
/// <see cref="LearningTask"/>s from these when a teacher assigns a lesson's tasks.
/// Mirrors LearningTask's blueprint fields, keyed to the lesson instead of an
/// assignment.
/// </summary>
public sealed class LessonDefaultTask : BaseEntity
{
    private LessonDefaultTask() { }

    public LessonDefaultTask(
        Guid curriculumLessonId,
        int order,
        LearningTaskType type,
        string title,
        int points,
        string contentJson,
        string? solutionJson = null)
    {
        if (curriculumLessonId == Guid.Empty) throw new DomainException("Curriculum lesson id is required.");
        if (order < 0) throw new DomainException("Order must be non-negative.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Task title is required.");
        if (points <= 0) throw new DomainException("Points must be greater than zero.");
        if (string.IsNullOrWhiteSpace(contentJson)) throw new DomainException("Content is required.");

        CurriculumLessonId = curriculumLessonId;
        Order = order;
        Type = type;
        Title = title.Trim();
        Points = points;
        ContentJson = contentJson;
        SolutionJson = string.IsNullOrWhiteSpace(solutionJson) ? null : solutionJson;
    }

    public Guid CurriculumLessonId { get; private set; }
    public CurriculumLesson? CurriculumLesson { get; private set; }

    public int Order { get; private set; }
    public LearningTaskType Type { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public int Points { get; private set; }
    /// <summary>JSON blob — type-specific. Same shapes as <see cref="LearningTask"/>.</summary>
    public string ContentJson { get; private set; } = "{}";
    /// <summary>JSON blob with the correct answer; null for manually-graded types.</summary>
    public string? SolutionJson { get; private set; }

    public void Update(int order, string title, int points, string contentJson, string? solutionJson)
    {
        if (order < 0) throw new DomainException("Order must be non-negative.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Task title is required.");
        if (points <= 0) throw new DomainException("Points must be greater than zero.");
        if (string.IsNullOrWhiteSpace(contentJson)) throw new DomainException("Content is required.");

        Order = order;
        Title = title.Trim();
        Points = points;
        ContentJson = contentJson;
        SolutionJson = string.IsNullOrWhiteSpace(solutionJson) ? null : solutionJson;
        Touch();
    }
}
