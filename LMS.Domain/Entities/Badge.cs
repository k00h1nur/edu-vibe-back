using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class Badge : BaseEntity
{
    public Badge(string name, int xpReward)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Badge name is required.");

        if (xpReward < 0) throw new DomainException("XP reward cannot be negative.");

        Name = name.Trim();
        XpReward = xpReward;
    }

    public string Name { get; private set; }
    public int XpReward { get; private set; }

    public ICollection<StudentBadge> StudentBadges { get; } = new List<StudentBadge>();

    public void Update(string name, int xpReward)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Badge name is required.");
        if (xpReward < 0) throw new DomainException("XP reward cannot be negative.");
        Name = name.Trim();
        XpReward = xpReward;
        Touch();
    }
}