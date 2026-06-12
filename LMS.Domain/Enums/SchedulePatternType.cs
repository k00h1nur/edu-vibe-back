namespace LMS.Domain.Enums;

/// <summary>
/// Recurring lesson pattern for a class. Odd/Even follow the local academy
/// convention: odd days = Mon/Wed/Fri, even days = Tue/Thu/Sat. Daily is
/// Mon–Sat (Sunday off); use Custom with an explicit weekday set when a
/// class really meets on Sundays.
/// </summary>
public enum SchedulePatternType
{
    /// <summary>Explicit weekday set from <c>DaysOfWeekMask</c>.</summary>
    Custom = 1,

    /// <summary>Mon / Wed / Fri.</summary>
    OddDays = 2,

    /// <summary>Tue / Thu / Sat.</summary>
    EvenDays = 3,

    /// <summary>Every day Mon–Sat.</summary>
    Daily = 4,
}
