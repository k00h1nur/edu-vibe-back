using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A self-check practice exercise attached to a curriculum lesson — textbook-style
/// homework (choose-the-correct, error-correction, gap-fill, word-bank, dialogue…).
///
/// DISTINCT from <see cref="LearningTask"/> (graded assignment items with the grader +
/// XP + manual-review pipeline): this is LESSON-scoped, tagged by a free-string
/// <see cref="Type"/>, and self-checked (no XP/teacher review). <see cref="ContentJson"/>
/// is jsonb whose shape varies by type — see the API/DTO docs. Adding a new type needs
/// only a new ExerciseChecker case + a frontend widget; this entity never changes.
/// </summary>
public sealed class LessonExercise : BaseEntity
{
    // EF materialisation ctor.
    private LessonExercise() { }

    public LessonExercise(Guid lessonId, string type, string title, int orderIndex, string contentJson)
    {
        if (lessonId == Guid.Empty) throw new DomainException("Lesson id is required.");
        if (string.IsNullOrWhiteSpace(type)) throw new DomainException("Exercise type is required.");
        if (orderIndex < 0) throw new DomainException("Order index must be non-negative.");
        if (string.IsNullOrWhiteSpace(contentJson)) throw new DomainException("Content is required.");

        LessonId = lessonId;
        Type = type.Trim();
        Title = title?.Trim() ?? string.Empty;
        OrderIndex = orderIndex;
        ContentJson = contentJson;
    }

    /// <summary>The curriculum lesson (e.g. "1A — People and places") this exercise belongs to.</summary>
    public Guid LessonId { get; private set; }

    /// <summary>Free-string discriminator: "mcq", "mcq_ab", "error_correction", "transform",
    /// "fill_blank", "word_bank_gap", "paragraph_cloze", "dialogue", … (extensible).</summary>
    public string Type { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;
    public int OrderIndex { get; private set; }

    /// <summary>jsonb — type-specific structure (items / parts / wordBank / answers). See API docs.</summary>
    public string ContentJson { get; private set; } = "{}";

    public ICollection<LessonExerciseSubmission> Submissions { get; } = new List<LessonExerciseSubmission>();

    public void Update(string type, string title, int orderIndex, string contentJson)
    {
        if (string.IsNullOrWhiteSpace(type)) throw new DomainException("Exercise type is required.");
        if (orderIndex < 0) throw new DomainException("Order index must be non-negative.");
        if (string.IsNullOrWhiteSpace(contentJson)) throw new DomainException("Content is required.");

        Type = type.Trim();
        Title = title?.Trim() ?? string.Empty;
        OrderIndex = orderIndex;
        ContentJson = contentJson;
        Touch();
    }
}
