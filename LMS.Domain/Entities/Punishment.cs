using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A salary deduction applied to a teacher for a specific month. Feeds the
/// monthly salary calculation: <see cref="PunishmentType.FixedAmount"/> is
/// subtracted directly; <see cref="PunishmentType.Percentage"/> is applied
/// against the computed salary. <see cref="PeriodMonth"/> is always normalised
/// to the 1st of the month (the platform-wide PeriodMonth convention).
/// </summary>
public sealed class Punishment : BaseEntity
{
    private Punishment() { } // EF

    public Punishment(
        Guid teacherId, string title, string? description, PunishmentType type,
        decimal value, string? reason, Guid appliedByAdminId, DateOnly periodMonth)
    {
        if (teacherId == Guid.Empty) throw new DomainException("Teacher is required.");
        if (appliedByAdminId == Guid.Empty) throw new DomainException("Applying admin is required.");
        TeacherId = teacherId;
        AppliedByAdminId = appliedByAdminId;
        PeriodMonth = FirstOfMonth(periodMonth);
        Set(title, description, type, value, reason);
    }

    public Guid TeacherId { get; private set; }
    public User? Teacher { get; private set; }
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public PunishmentType Type { get; private set; }
    /// <summary>If Percentage: 0–100 (% of salary). If FixedAmount: a money amount.</summary>
    public decimal Value { get; private set; }
    public string? Reason { get; private set; }
    /// <summary>The admin (User id) who applied this. Audit only — no navigation.</summary>
    public Guid AppliedByAdminId { get; private set; }
    /// <summary>Always the 1st of the target month.</summary>
    public DateOnly PeriodMonth { get; private set; }

    public void Update(string title, string? description, PunishmentType type, decimal value,
        string? reason, DateOnly periodMonth)
    {
        Set(title, description, type, value, reason);
        PeriodMonth = FirstOfMonth(periodMonth);
        Touch();
    }

    private void Set(string title, string? description, PunishmentType type, decimal value, string? reason)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Title is required.");
        if (value < 0m) throw new DomainException("Value must be non-negative.");
        if (type == PunishmentType.Percentage && value > 100m)
            throw new DomainException("A percentage punishment can't exceed 100%.");
        Title = title.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Type = type;
        Value = value;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    private static DateOnly FirstOfMonth(DateOnly d) => new(d.Year, d.Month, 1);
}
