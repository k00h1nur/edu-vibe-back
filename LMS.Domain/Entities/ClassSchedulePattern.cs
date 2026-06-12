using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// The recurring lesson pattern for a class — one per class. Concrete
/// <see cref="ClassSession"/> rows are GENERATED from this pattern by the
/// ApplyClassSchedule handler; everything downstream (teacher/student
/// schedules, attendance) keeps reading sessions and never needs to know
/// the pattern exists.
/// </summary>
public sealed class ClassSchedulePattern : BaseEntity
{
    /// <summary>Monday..Saturday bits — used when deriving masks for the preset types.</summary>
    public const int MondayBit = 1 << 0;
    public const int TuesdayBit = 1 << 1;
    public const int WednesdayBit = 1 << 2;
    public const int ThursdayBit = 1 << 3;
    public const int FridayBit = 1 << 4;
    public const int SaturdayBit = 1 << 5;
    public const int SundayBit = 1 << 6;
    public const int AllSevenDays = (1 << 7) - 1;

    private ClassSchedulePattern()
    {
        // EF materialization.
    }

    public ClassSchedulePattern(
        Guid classId,
        SchedulePatternType type,
        int daysOfWeekMask,
        DateOnly startDate,
        DateOnly endDate,
        TimeOnly startsAt,
        TimeOnly endsAt,
        Guid? roomId = null)
    {
        if (classId == Guid.Empty) throw new DomainException("Class id is required.");
        ClassId = classId;
        Update(type, daysOfWeekMask, startDate, endDate, startsAt, endsAt, roomId);
    }

    public Guid ClassId { get; private set; }
    public Class? Class { get; private set; }

    public SchedulePatternType Type { get; private set; }

    /// <summary>
    /// Bitmask of weekdays the class meets (bit 0 = Monday … bit 6 = Sunday).
    /// Authoritative for <see cref="SchedulePatternType.Custom"/>; for the
    /// preset types it's stored pre-derived so generation never re-interprets
    /// the enum.
    /// </summary>
    public int DaysOfWeekMask { get; private set; }

    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public TimeOnly StartsAt { get; private set; }
    public TimeOnly EndsAt { get; private set; }
    public Guid? RoomId { get; private set; }

    public void Update(
        SchedulePatternType type,
        int daysOfWeekMask,
        DateOnly startDate,
        DateOnly endDate,
        TimeOnly startsAt,
        TimeOnly endsAt,
        Guid? roomId)
    {
        if (endDate < startDate) throw new DomainException("EndDate must be on or after StartDate.");
        if (startsAt >= endsAt) throw new DomainException("StartsAt must be before EndsAt.");

        var mask = type switch
        {
            SchedulePatternType.OddDays => MondayBit | WednesdayBit | FridayBit,
            SchedulePatternType.EvenDays => TuesdayBit | ThursdayBit | SaturdayBit,
            SchedulePatternType.Daily => AllSevenDays & ~SundayBit,
            _ => daysOfWeekMask & AllSevenDays,
        };
        if (mask == 0) throw new DomainException("Pick at least one weekday.");

        Type = type;
        DaysOfWeekMask = mask;
        StartDate = startDate;
        EndDate = endDate;
        StartsAt = startsAt;
        EndsAt = endsAt;
        RoomId = roomId;
        Touch();
    }

    /// <summary>True when the pattern includes the given calendar date's weekday.</summary>
    public bool Matches(DateOnly date)
    {
        // DayOfWeek: Sunday=0..Saturday=6 → our mask: Monday=bit0..Sunday=bit6.
        var bit = date.DayOfWeek == DayOfWeek.Sunday ? SundayBit : 1 << ((int)date.DayOfWeek - 1);
        return (DaysOfWeekMask & bit) != 0;
    }
}
