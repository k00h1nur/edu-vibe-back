using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A reusable, teacher-authored collection of practice exercises — independent of the
/// curriculum. A teacher builds a set once (adding exercises via the same authoring
/// engine as lesson exercises) and attaches it to one or more classes
/// (<see cref="ExerciseSetClass"/>); every student in an attached class can then work
/// through it. Exercises live in <c>lesson_exercises</c> with <c>ExerciseSetId</c> set
/// (instead of <c>LessonId</c>), so the whole submit / self-check / grade / XP engine is
/// reused verbatim.
/// </summary>
public sealed class ExerciseSet : BaseEntity
{
    // EF materialisation ctor.
    private ExerciseSet() { }

    public ExerciseSet(string title, string? description, Guid createdByUserId)
    {
        if (createdByUserId == Guid.Empty) throw new DomainException("Creator user id is required.");
        CreatedByUserId = createdByUserId;
        SetDetails(title, description);
    }

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    /// <summary>The teacher (or admin) who owns/authored the set.</summary>
    public Guid CreatedByUserId { get; private set; }

    /// <summary>Classes this set is attached to — its presence is the grant to that class's students.</summary>
    public ICollection<ExerciseSetClass> ClassLinks { get; } = new List<ExerciseSetClass>();

    public void SetDetails(string title, string? description)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Title is required.");
        var t = title.Trim();
        if (t.Length > 200) throw new DomainException("Title must be 200 characters or fewer.");
        Title = t;

        if (string.IsNullOrWhiteSpace(description))
        {
            Description = null;
        }
        else
        {
            var d = description.Trim();
            Description = d.Length > 2000 ? d[..2000] : d;
        }
        Touch();
    }
}
