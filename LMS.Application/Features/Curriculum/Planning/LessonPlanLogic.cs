namespace LMS.Application.Features.Curriculum.Planning;

// Pure, DB-free planning logic for the template-level teaching plan + the
// multi-lesson session model. Mirrors the #51 style (LessonTaskMaterialization /
// SchedulePreserve): all rules live here as pure functions so they're unit-tested
// before any migration or handler touches live data. The wiring PRs (clone-copy,
// plan-aware generation, roadmap, materialize-on-confirm) call into these.

/// <summary>A blueprint task identified by its source lesson + order — the unit of a day's homework union.</summary>
public readonly record struct TaskRef(Guid LessonId, int Order);

/// <summary>One lesson's blueprint task orders (e.g. lesson 1A has tasks [1,2,3]).</summary>
public sealed record LessonTasks(Guid LessonId, IReadOnlyList<int> TaskOrders);

/// <summary>A plan-day and the curriculum lessons it covers, in order (1A=1, 1B=2; an exam day = one lesson).</summary>
public sealed record PlanDayLessons(Guid PlanDayId, int Order, IReadOnlyList<Guid> LessonIds);

/// <summary>
/// Outcome of reconciling a session's materialized lesson set after a teacher
/// re-confirms it. <see cref="ToMaterialize"/> = newly added lessons whose tasks
/// must be created; <see cref="ToDeletable"/> = removed lessons whose tasks have
/// NO student submissions (safe to delete); <see cref="ToPreserve"/> = removed
/// lessons whose tasks HAVE submissions (kept — never destroy student work).
/// </summary>
public sealed record ReconcileResult(
    IReadOnlyList<Guid> ToMaterialize,
    IReadOnlyList<Guid> ToDeletable,
    IReadOnlyList<Guid> ToPreserve);

/// <summary>Per-day completion + overall progress for the student roadmap (derived, never stored).</summary>
public sealed record PlanDayProgress(Guid PlanDayId, int Order, bool Completed);
public sealed record PlanProgress(IReadOnlyList<PlanDayProgress> Days, int CompletedDays, int TotalDays, int ProgressPct);

public static class LessonPlanLogic
{
    /// <summary>
    /// Positional plan-day → session pairing, in chronological/Order order, for
    /// i &lt; min(#sessions, #plan-days). Surplus sessions or surplus plan-days are
    /// left unpaired (the min() behaviour we accepted). Returns (SessionId, PlanDayId).
    /// </summary>
    public static IReadOnlyList<(Guid SessionId, Guid PlanDayId)> MapPlanDaysToSessions(
        IReadOnlyList<Guid> orderedSessionIds,
        IReadOnlyList<Guid> orderedPlanDayIds)
    {
        var n = Math.Min(orderedSessionIds.Count, orderedPlanDayIds.Count);
        var pairs = new List<(Guid, Guid)>(n);
        for (var i = 0; i < n; i++)
            pairs.Add((orderedSessionIds[i], orderedPlanDayIds[i]));
        return pairs;
    }

    /// <summary>
    /// A plan-day's homework = the UNION of its lessons' blueprint tasks, kept in
    /// (lesson order, task order) and de-duplicated by (LessonId, Order) so the
    /// same lesson listed twice can't double a task. An exam day (a lesson with no
    /// tasks) contributes nothing.
    /// </summary>
    public static IReadOnlyList<TaskRef> PairedHomeworkUnion(IReadOnlyList<LessonTasks> dayLessons)
    {
        var seen = new HashSet<TaskRef>();
        var union = new List<TaskRef>();
        foreach (var lesson in dayLessons)
            foreach (var order in lesson.TaskOrders)
            {
                var key = new TaskRef(lesson.LessonId, order);
                if (seen.Add(key)) union.Add(key);
            }
        return union;
    }

    /// <summary>
    /// Reconcile a session's lesson set on teacher re-confirm. Added lessons →
    /// materialize; removed lessons split by whether their tasks already hold
    /// student submissions: no submissions → deletable, has submissions → preserve.
    /// Same set in/out → all three empty (idempotent no-op). Order of the new set
    /// is preserved for ToMaterialize so tasks materialize in a stable order.
    /// </summary>
    public static ReconcileResult ReconcilePlan(
        IReadOnlyCollection<Guid> oldLessonIds,
        IReadOnlyCollection<Guid> newLessonIds,
        IReadOnlyCollection<Guid> lessonIdsWithSubmissions)
    {
        var oldSet = oldLessonIds as ISet<Guid> ?? new HashSet<Guid>(oldLessonIds);
        var newSet = newLessonIds as ISet<Guid> ?? new HashSet<Guid>(newLessonIds);
        var withSubs = lessonIdsWithSubmissions as ISet<Guid> ?? new HashSet<Guid>(lessonIdsWithSubmissions);

        var toMaterialize = newLessonIds.Where(id => !oldSet.Contains(id)).Distinct().ToList();

        var removed = oldLessonIds.Where(id => !newSet.Contains(id)).Distinct().ToList();
        var toDeletable = removed.Where(id => !withSubs.Contains(id)).ToList();
        var toPreserve = removed.Where(id => withSubs.Contains(id)).ToList();

        return new ReconcileResult(toMaterialize, toDeletable, toPreserve);
    }

    /// <summary>
    /// Plan-day progress for the roadmap. A day is complete only when EVERY lesson
    /// it covers is in the completed set (so a paired 1A+1B day needs both done; an
    /// exam day needs its one lesson). A day with no lessons is never "complete".
    /// 100% is reachable: all days complete ⇒ pct 100.
    /// </summary>
    public static PlanProgress ComputePlanProgress(
        IReadOnlyList<PlanDayLessons> days,
        IReadOnlyCollection<Guid> completedLessonIds)
    {
        var done = completedLessonIds as ISet<Guid> ?? new HashSet<Guid>(completedLessonIds);

        var perDay = days
            .OrderBy(d => d.Order)
            .Select(d => new PlanDayProgress(
                d.PlanDayId, d.Order,
                d.LessonIds.Count > 0 && d.LessonIds.All(done.Contains)))
            .ToList();

        var completedDays = perDay.Count(d => d.Completed);
        var total = perDay.Count;
        var pct = total == 0 ? 0 : (int)Math.Round(completedDays * 100.0 / total, MidpointRounding.AwayFromZero);
        return new PlanProgress(perDay, completedDays, total, pct);
    }

    /// <summary>
    /// Which (session, lesson) join rows the backfill must insert: one per session
    /// that has a primary lesson and isn't already present. Models the migration's
    /// idempotent backfill — re-running with everything already present returns
    /// nothing.
    /// </summary>
    public static IReadOnlyList<(Guid SessionId, Guid LessonId)> SessionLessonsToBackfill(
        IReadOnlyList<(Guid SessionId, Guid PrimaryLessonId)> sessionsWithPrimary,
        ISet<(Guid SessionId, Guid LessonId)> existing)
    {
        var rows = new List<(Guid, Guid)>();
        foreach (var (sessionId, lessonId) in sessionsWithPrimary)
            if (existing.Add((sessionId, lessonId)))
                rows.Add((sessionId, lessonId));
        return rows;
    }
}
