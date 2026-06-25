namespace LMS.Application.Common;

/// <summary>
/// Single source of truth for "the school day" and lesson-homework visibility.
/// The academy operates in Tashkent (UTC+5, no DST). Homework for a lesson
/// unlocks to students at local midnight on the lesson day — never a day early,
/// never hours late.
///
/// MULTI-TENANT: when centers can live in different zones, replace
/// <see cref="SchoolUtcOffset"/> with a per-tenant timezone lookup. This is the
/// ONE place the offset and the visibility rule live, so list / read / submit
/// can never drift apart and the swap is a single-spot change.
/// </summary>
public static class SchoolCalendar
{
    /// <summary>Tashkent local offset from UTC. The only place +5 is hardcoded.</summary>
    public static readonly TimeSpan SchoolUtcOffset = TimeSpan.FromHours(5);

    /// <summary>Today's calendar date in the school's local zone.</summary>
    public static DateOnly Today(DateTime utcNow) => DateOnly.FromDateTime(utcNow + SchoolUtcOffset);

    /// <summary>
    /// A lesson's homework is visible to a STUDENT once the lesson day has arrived
    /// (school-local). An assignment not tied to a dated lesson (<paramref name="lessonDate"/>
    /// null) is always visible. Staff are never gated — callers apply this for students only.
    /// </summary>
    public static bool IsLessonHomeworkVisibleToStudent(DateOnly? lessonDate, DateOnly schoolToday)
        => lessonDate is null || lessonDate.Value <= schoolToday;
}
