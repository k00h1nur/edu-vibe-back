using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A student's completion mark for one lesson (ClassSession). One row per
/// (student, session) — presence means "completed at CompletedAt". Un-marking
/// deletes the row.
/// </summary>
public sealed class LessonProgress : BaseEntity
{
    private LessonProgress() { }

    public LessonProgress(Guid studentProfileId, Guid classSessionId)
    {
        if (studentProfileId == Guid.Empty) throw new DomainException("Student profile id is required.");
        if (classSessionId == Guid.Empty) throw new DomainException("Class session id is required.");
        StudentProfileId = studentProfileId;
        ClassSessionId = classSessionId;
        CompletedAt = DateTime.UtcNow;
    }

    public Guid StudentProfileId { get; private set; }
    public Guid ClassSessionId { get; private set; }
    public DateTime CompletedAt { get; private set; }
}
