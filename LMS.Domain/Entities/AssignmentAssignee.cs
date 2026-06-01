using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// Targets an assignment at a SPECIFIC student rather than the whole class.
/// If an assignment has zero <see cref="AssignmentAssignee"/> rows, it's
/// implicitly visible to every enrolled student in <see cref="Assignment.ClassId"/>.
/// </summary>
public sealed class AssignmentAssignee : BaseEntity
{
    private AssignmentAssignee() { }

    public AssignmentAssignee(Guid assignmentId, Guid studentProfileId)
    {
        if (assignmentId == Guid.Empty) throw new DomainException("Assignment id is required.");
        if (studentProfileId == Guid.Empty) throw new DomainException("Student profile id is required.");
        AssignmentId = assignmentId;
        StudentProfileId = studentProfileId;
    }

    public Guid AssignmentId { get; private set; }
    public Assignment? Assignment { get; private set; }
    public Guid StudentProfileId { get; private set; }
    public StudentProfile? StudentProfile { get; private set; }
}
