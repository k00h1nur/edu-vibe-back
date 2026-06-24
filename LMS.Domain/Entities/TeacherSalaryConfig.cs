using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// The revenue share a teacher earns. A class-specific row (<see cref="ClassId"/>
/// set) overrides the teacher-wide default (<see cref="ClassId"/> null) in the
/// salary calculation. <see cref="Percentage"/> is 0–100. Uniqueness on
/// (TeacherId, ClassId) is enforced by a NULLS-NOT-DISTINCT index so there's at
/// most one default + one per class.
/// </summary>
public sealed class TeacherSalaryConfig : BaseEntity
{
    private TeacherSalaryConfig() { } // EF

    public TeacherSalaryConfig(Guid teacherId, Guid? classId, decimal percentage)
    {
        if (teacherId == Guid.Empty) throw new DomainException("Teacher is required.");
        TeacherId = teacherId;
        ClassId = classId;
        SetPercentage(percentage);
    }

    public Guid TeacherId { get; private set; }
    public User? Teacher { get; private set; }
    /// <summary>Null = the teacher's default share; set = a per-class override.</summary>
    public Guid? ClassId { get; private set; }
    public Class? Class { get; private set; }
    public decimal Percentage { get; private set; }

    public void SetPercentage(decimal percentage)
    {
        if (percentage < 0m || percentage > 100m)
            throw new DomainException("Percentage must be between 0 and 100.");
        Percentage = percentage;
        Touch();
    }
}
