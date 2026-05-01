using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class StudentBadge : BaseEntity
{
    private StudentBadge(Guid studentProfileId, Guid badgeId)
    {
        if (studentProfileId == Guid.Empty || badgeId == Guid.Empty)
            throw new DomainException("Student profile and badge ids are required.");

        StudentProfileId = studentProfileId;
        BadgeId = badgeId;
        AwardedAt = DateTime.UtcNow;
    }

    public Guid StudentProfileId { get; }
    public StudentProfile? StudentProfile { get; private set; }

    public Guid BadgeId { get; }
    public Badge? Badge { get; private set; }

    public DateTime AwardedAt { get; private set; }

    public static StudentBadge Award(Guid studentProfileId, Guid badgeId, IEnumerable<StudentBadge> existingBadges)
    {
        if (existingBadges.Any(x => x.StudentProfileId == studentProfileId && x.BadgeId == badgeId))
            throw new DomainException("Student already has this badge.");

        return new StudentBadge(studentProfileId, badgeId);
    }
}