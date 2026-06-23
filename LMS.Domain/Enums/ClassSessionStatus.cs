namespace LMS.Domain.Enums;

/// <summary>The teaching lifecycle of a scheduled session, set by the teacher.</summary>
public enum ClassSessionStatus
{
    Planned = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
}
