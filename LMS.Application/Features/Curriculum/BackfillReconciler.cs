namespace LMS.Application.Features.Curriculum;

/// <summary>One existing curriculum-linked session, as the reconciler sees it (no EF types).</summary>
public sealed record BackfillSession(Guid SessionId, Guid CurriculumLessonId, bool IsBackfilled, bool HasAttendance);

/// <summary>The reconcile plan for F6 set-position. Pure output; the handler applies it.</summary>
public sealed record BackfillPlan(
    IReadOnlyList<Guid> CreateForLessonIds,
    IReadOnlyList<Guid> DeleteSessionIds,
    IReadOnlyList<Guid> SkippedWithAttendance);

/// <summary>
/// Pure, DB-free reconcile for F6 existing-group onboarding. It declares the exact
/// set of backfilled-completed lessons to equal the target set (the lessons before
/// the chosen position):
///  • a target lesson with NO session of any kind → create a backfilled session;
///  • a backfilled session whose lesson is NOT in the target → delete it, UNLESS it
///    has attendance, in which case it's reported and left in place (the safety belt);
///  • real (non-backfilled) sessions are never created, deleted, or mutated, and a
///    target lesson already covered by any session is left alone.
/// Idempotent: re-running with the same target yields an empty plan.
/// </summary>
public static class BackfillReconciler
{
    public static BackfillPlan Plan(
        IReadOnlyList<Guid> targetLessonIds,
        IReadOnlyCollection<BackfillSession> existingLinkedSessions)
    {
        var target = targetLessonIds.ToHashSet();
        var lessonsWithAnySession = existingLinkedSessions.Select(s => s.CurriculumLessonId).ToHashSet();

        // Create a backfilled session only for target lessons that have no session
        // at all — never override a real (or already-backfilled) session.
        var create = targetLessonIds
            .Where(id => !lessonsWithAnySession.Contains(id))
            .Distinct()
            .ToList();

        var delete = new List<Guid>();
        var skipped = new List<Guid>();
        foreach (var s in existingLinkedSessions)
        {
            // Only backfilled sessions outside the target are candidates for removal;
            // real sessions and in-target backfill are always kept.
            if (!s.IsBackfilled || target.Contains(s.CurriculumLessonId)) continue;
            if (s.HasAttendance) skipped.Add(s.SessionId); // safety belt — never destroy attendance
            else delete.Add(s.SessionId);
        }

        return new BackfillPlan(create, delete, skipped);
    }
}
