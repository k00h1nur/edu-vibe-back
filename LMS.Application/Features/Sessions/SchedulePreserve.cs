namespace LMS.Application.Features.Sessions;

/// <summary>A future session reduced to what the preserve decision needs.</summary>
public sealed record ScheduleSessionRef(Guid Id, DateOnly Date);

/// <summary>Which future sessions are safe to delete + which dates are already taken.</summary>
public sealed record SchedulePreservePlan(IReadOnlyList<Guid> RemovableIds, IReadOnlySet<DateOnly> OccupiedDates);

/// <summary>
/// Pure core of the reschedule preserve rule. A future session is PRESERVED (never
/// deleted by a re-apply/generate) when it carries real dependent data — attendance
/// marks OR materialised homework. Its date is then "occupied" so the regenerate
/// step skips it and the (ClassId, SessionDate, StartsAt) unique index can't collide.
///
/// DB-free so "re-running keeps homework'd/attendance'd sessions" is unit-tested;
/// the handler just feeds it the protected-id set it builds from the DB.
/// </summary>
public static class SchedulePreserve
{
    public static SchedulePreservePlan Partition(
        IReadOnlyList<ScheduleSessionRef> futureSessions, IReadOnlySet<Guid> protectedIds)
    {
        var removable = futureSessions.Where(s => !protectedIds.Contains(s.Id)).Select(s => s.Id).ToList();
        var occupied = futureSessions.Where(s => protectedIds.Contains(s.Id)).Select(s => s.Date).ToHashSet();
        return new SchedulePreservePlan(removable, occupied);
    }
}
