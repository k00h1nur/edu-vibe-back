using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// Join row attaching an <see cref="ExerciseSet"/> to a <see cref="Class"/>. Its presence
/// is the grant: every enrolled student of the class can work through the set. One set can
/// be attached to many classes (reusable), and a class can hold many sets.
/// </summary>
public sealed class ExerciseSetClass : BaseEntity
{
    private ExerciseSetClass() { }

    public ExerciseSetClass(Guid exerciseSetId, Guid classId)
    {
        if (exerciseSetId == Guid.Empty) throw new DomainException("Exercise set id is required.");
        if (classId == Guid.Empty) throw new DomainException("Class id is required.");
        ExerciseSetId = exerciseSetId;
        ClassId = classId;
    }

    public Guid ExerciseSetId { get; private set; }
    public Guid ClassId { get; private set; }
}
