using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class StudentProfile : BaseEntity
{
    public StudentProfile(Guid userId, User user)
    {
        if (userId == Guid.Empty) throw new DomainException("Student user id is required.");

        UserId = userId;
        User = user ?? throw new DomainException("User is required.");
        XP = 0;
        Streak = 0;
    }

    public Guid UserId { get; private set; }
    public User User { get; private set; }

    public int XP { get; private set; }
    public int Streak { get; private set; }

    public ICollection<Enrollment> Enrollments { get; } = new List<Enrollment>();
    public ICollection<Attendance> Attendances { get; } = new List<Attendance>();
    public ICollection<Submission> Submissions { get; } = new List<Submission>();
    public ICollection<StudentBadge> StudentBadges { get; } = new List<StudentBadge>();
    public ICollection<XpLedger> XpLedgerEntries { get; } = new List<XpLedger>();

    public void AddXp(int amount)
    {
        if (amount <= 0) throw new DomainException("XP amount must be greater than zero.");

        XP += amount;
        Touch();
    }

    public void UpdateStreak(int newStreak)
    {
        if (newStreak < 0) throw new DomainException("Streak cannot be negative.");

        Streak = newStreak;
        Touch();
    }
}