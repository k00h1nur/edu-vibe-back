using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class Enrollment : BaseEntity
{
    private Enrollment(Guid classId, Guid studentProfileId)
    {
        if (classId == Guid.Empty || studentProfileId == Guid.Empty)
            throw new DomainException("Class and student profile ids are required.");

        ClassId = classId;
        StudentProfileId = studentProfileId;
        Status = EnrollmentStatus.Active;
        EnrolledAt = DateTime.UtcNow;
    }

    public Guid ClassId { get; }
    public Class? Class { get; private set; }

    public Guid StudentProfileId { get; }
    public StudentProfile? StudentProfile { get; private set; }

    public EnrollmentStatus Status { get; private set; }
    public DateTime EnrolledAt { get; private set; }

    public static Enrollment Create(Guid classId, Guid studentProfileId, IEnumerable<Enrollment> existingEnrollments)
    {
        if (existingEnrollments.Any(x =>
                x.ClassId == classId && x.StudentProfileId == studentProfileId && x.Status != EnrollmentStatus.Dropped))
            throw new DomainException("Duplicate enrollment is not allowed.");

        return new Enrollment(classId, studentProfileId);
    }

    public void Activate()
    {
        if (Status == EnrollmentStatus.Active) throw new DomainException("Enrollment is already active.");

        Status = EnrollmentStatus.Active;
        Touch();
    }

    public void Drop()
    {
        if (Status == EnrollmentStatus.Dropped) throw new DomainException("Enrollment is already dropped.");

        Status = EnrollmentStatus.Dropped;
        Touch();
    }
}