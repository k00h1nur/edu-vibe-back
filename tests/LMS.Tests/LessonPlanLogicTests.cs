using FluentAssertions;
using LMS.Application.Features.Curriculum.Planning;
using Xunit;

namespace LMS.Tests;

/// <summary>
/// Pure planning logic for the template plan + multi-lesson sessions (PR1 rails).
/// Proves the submission-safety reconcile + the mapping/progress rules BEFORE any
/// migration or handler touches live data — same discipline as the #51 pure tests.
/// </summary>
public sealed class LessonPlanLogicTests
{
    private static Guid Id() => Guid.NewGuid();

    // ---- MapPlanDaysToSessions ------------------------------------------

    [Fact]
    public void Map_pairs_one_to_one_when_counts_match()
    {
        var s = new[] { Id(), Id(), Id() };
        var d = new[] { Id(), Id(), Id() };
        var pairs = LessonPlanLogic.MapPlanDaysToSessions(s, d);
        pairs.Should().Equal((s[0], d[0]), (s[1], d[1]), (s[2], d[2]));
    }

    [Fact]
    public void Map_caps_at_fewer_days_when_more_sessions()
    {
        var s = new[] { Id(), Id(), Id(), Id() }; // 4 sessions
        var d = new[] { Id(), Id() };             // 2 plan-days
        var pairs = LessonPlanLogic.MapPlanDaysToSessions(s, d);
        pairs.Should().HaveCount(2);
        pairs.Should().Equal((s[0], d[0]), (s[1], d[1])); // surplus sessions unpaired
    }

    [Fact]
    public void Map_caps_at_fewer_sessions_when_more_days()
    {
        var s = new[] { Id() };          // 1 session
        var d = new[] { Id(), Id(), Id() }; // 3 plan-days
        LessonPlanLogic.MapPlanDaysToSessions(s, d).Should().HaveCount(1); // days 2,3 never get a session
    }

    [Fact]
    public void Map_empty_inputs_yield_empty()
    {
        LessonPlanLogic.MapPlanDaysToSessions(Array.Empty<Guid>(), new[] { Id() }).Should().BeEmpty();
        LessonPlanLogic.MapPlanDaysToSessions(new[] { Id() }, Array.Empty<Guid>()).Should().BeEmpty();
    }

    // ---- PairedHomeworkUnion --------------------------------------------

    [Fact]
    public void Union_combines_two_lessons_in_order()
    {
        var a = Id(); var b = Id();
        var union = LessonPlanLogic.PairedHomeworkUnion(new[]
        {
            new LessonTasks(a, new[] { 1, 2 }),
            new LessonTasks(b, new[] { 1, 2, 3 }),
        });
        union.Should().Equal(
            new TaskRef(a, 1), new TaskRef(a, 2),
            new TaskRef(b, 1), new TaskRef(b, 2), new TaskRef(b, 3));
    }

    [Fact]
    public void Union_dedupes_same_lesson_and_order()
    {
        var a = Id();
        var union = LessonPlanLogic.PairedHomeworkUnion(new[]
        {
            new LessonTasks(a, new[] { 1, 2 }),
            new LessonTasks(a, new[] { 2, 3 }), // 2 repeats → counted once
        });
        union.Should().Equal(new TaskRef(a, 1), new TaskRef(a, 2), new TaskRef(a, 3));
    }

    [Fact]
    public void Union_of_exam_day_with_no_tasks_is_empty()
    {
        var exam = Id();
        LessonPlanLogic.PairedHomeworkUnion(new[] { new LessonTasks(exam, Array.Empty<int>()) })
            .Should().BeEmpty();
    }

    // ---- ReconcilePlan — the four submission-safety cases ----------------

    [Fact]
    public void Reconcile_add_only_materializes_the_new_lesson()
    {
        var a = Id(); var b = Id();
        var r = LessonPlanLogic.ReconcilePlan(
            oldLessonIds: new[] { a },
            newLessonIds: new[] { a, b },
            lessonIdsWithSubmissions: Array.Empty<Guid>());
        r.ToMaterialize.Should().Equal(b);
        r.ToDeletable.Should().BeEmpty();
        r.ToPreserve.Should().BeEmpty();
    }

    [Fact]
    public void Reconcile_remove_without_submissions_is_deletable()
    {
        var a = Id(); var b = Id();
        var r = LessonPlanLogic.ReconcilePlan(
            oldLessonIds: new[] { a, b },
            newLessonIds: new[] { a },
            lessonIdsWithSubmissions: Array.Empty<Guid>());
        r.ToMaterialize.Should().BeEmpty();
        r.ToDeletable.Should().Equal(b);
        r.ToPreserve.Should().BeEmpty();
    }

    [Fact]
    public void Reconcile_remove_with_submissions_is_preserved_never_deleted()
    {
        var a = Id(); var b = Id();
        var r = LessonPlanLogic.ReconcilePlan(
            oldLessonIds: new[] { a, b },
            newLessonIds: new[] { a },
            lessonIdsWithSubmissions: new[] { b }); // b has student work
        r.ToMaterialize.Should().BeEmpty();
        r.ToDeletable.Should().BeEmpty();          // NOT deleted
        r.ToPreserve.Should().Equal(b);            // kept
    }

    [Fact]
    public void Reconcile_same_set_is_a_no_op()
    {
        var a = Id(); var b = Id();
        var r = LessonPlanLogic.ReconcilePlan(new[] { a, b }, new[] { b, a }, new[] { a });
        r.ToMaterialize.Should().BeEmpty();
        r.ToDeletable.Should().BeEmpty();
        r.ToPreserve.Should().BeEmpty();
    }

    [Fact]
    public void Reconcile_mixed_add_and_remove_with_submissions()
    {
        var a = Id(); var b = Id(); var c = Id();
        // was {a,b}; now {a,c}; b has submissions → add c, preserve b, delete nothing.
        var r = LessonPlanLogic.ReconcilePlan(new[] { a, b }, new[] { a, c }, new[] { b });
        r.ToMaterialize.Should().Equal(c);
        r.ToDeletable.Should().BeEmpty();
        r.ToPreserve.Should().Equal(b);
    }

    // ---- ComputePlanProgress --------------------------------------------

    [Fact]
    public void Progress_paired_day_completes_only_when_both_lessons_done()
    {
        var d1 = Id(); var l1a = Id(); var l1b = Id();
        var days = new[] { new PlanDayLessons(d1, 1, new[] { l1a, l1b }) };

        LessonPlanLogic.ComputePlanProgress(days, new[] { l1a }).Days[0].Completed
            .Should().BeFalse("only one of the paired lessons is done");
        LessonPlanLogic.ComputePlanProgress(days, new[] { l1a, l1b }).Days[0].Completed
            .Should().BeTrue("both paired lessons are done");
    }

    [Fact]
    public void Progress_exam_day_completes_on_its_single_lesson()
    {
        var d = Id(); var exam = Id();
        var p = LessonPlanLogic.ComputePlanProgress(new[] { new PlanDayLessons(d, 6, new[] { exam }) }, new[] { exam });
        p.Days[0].Completed.Should().BeTrue();
        p.ProgressPct.Should().Be(100);
    }

    [Fact]
    public void Progress_reaches_100_percent_across_24_days()
    {
        var days = new List<PlanDayLessons>();
        var allLessons = new List<Guid>();
        for (var i = 1; i <= 24; i++)
        {
            var a = Id(); var b = Id();
            days.Add(new PlanDayLessons(Id(), i, new[] { a, b }));
            allLessons.Add(a); allLessons.Add(b);
        }

        var none = LessonPlanLogic.ComputePlanProgress(days, Array.Empty<Guid>());
        none.CompletedDays.Should().Be(0);
        none.ProgressPct.Should().Be(0);

        var all = LessonPlanLogic.ComputePlanProgress(days, allLessons);
        all.CompletedDays.Should().Be(24);
        all.TotalDays.Should().Be(24);
        all.ProgressPct.Should().Be(100); // 100% IS reachable — the 74-vs-24 trap is gone
    }

    [Fact]
    public void Progress_partial_rounds_correctly()
    {
        var days = new List<PlanDayLessons>();
        var done = new List<Guid>();
        for (var i = 1; i <= 4; i++)
        {
            var l = Id();
            days.Add(new PlanDayLessons(Id(), i, new[] { l }));
            if (i <= 1) done.Add(l); // 1 of 4 done = 25%
        }
        LessonPlanLogic.ComputePlanProgress(days, done).ProgressPct.Should().Be(25);
    }

    [Fact]
    public void Progress_day_with_no_lessons_never_completes()
    {
        var d = Id();
        var p = LessonPlanLogic.ComputePlanProgress(new[] { new PlanDayLessons(d, 1, Array.Empty<Guid>()) }, Array.Empty<Guid>());
        p.Days[0].Completed.Should().BeFalse();
        p.ProgressPct.Should().Be(0);
    }

    // ---- SessionLessonsToBackfill (idempotency) -------------------------

    [Fact]
    public void Backfill_returns_only_missing_rows()
    {
        var s1 = Id(); var s2 = Id(); var l1 = Id(); var l2 = Id();
        var existing = new HashSet<(Guid, Guid)> { (s1, l1) }; // s1 already backfilled
        var rows = LessonPlanLogic.SessionLessonsToBackfill(
            new[] { (s1, l1), (s2, l2) }, existing);
        rows.Should().Equal((s2, l2)); // only the missing one
    }

    [Fact]
    public void Backfill_rerun_with_all_present_is_empty()
    {
        var s1 = Id(); var l1 = Id();
        var existing = new HashSet<(Guid, Guid)>();
        var first = LessonPlanLogic.SessionLessonsToBackfill(new[] { (s1, l1) }, existing);
        first.Should().Equal((s1, l1));
        // existing now contains (s1,l1) (the helper added it) → second run inserts nothing.
        var second = LessonPlanLogic.SessionLessonsToBackfill(new[] { (s1, l1) }, existing);
        second.Should().BeEmpty();
    }
}
