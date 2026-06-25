using FluentAssertions;
using LMS.Application.Features.Curriculum;
using Xunit;

namespace LMS.Tests;

/// <summary>
/// F6 reconcile guard — the live-data safety core. Proves set-position is
/// idempotent, only ever touches backfilled sessions, respects real sessions, and
/// honours the attendance safety belt. (Cases a–f from the build guardrails.)
/// </summary>
public sealed class BackfillReconcilerTests
{
    private static readonly Guid L1 = Guid.NewGuid();
    private static readonly Guid L2 = Guid.NewGuid();
    private static readonly Guid L3 = Guid.NewGuid();

    private static BackfillSession Backfilled(Guid lessonId, bool hasAttendance = false) =>
        new(Guid.NewGuid(), lessonId, IsBackfilled: true, HasAttendance: hasAttendance);

    private static BackfillSession Real(Guid lessonId, bool hasAttendance = false) =>
        new(Guid.NewGuid(), lessonId, IsBackfilled: false, HasAttendance: hasAttendance);

    // (a) re-run with the same position = no-op.
    [Fact]
    public void Rerun_same_position_is_a_noop()
    {
        var plan = BackfillReconciler.Plan(
            new[] { L1, L2 },
            new[] { Backfilled(L1), Backfilled(L2) });

        plan.CreateForLessonIds.Should().BeEmpty();
        plan.DeleteSessionIds.Should().BeEmpty();
        plan.SkippedWithAttendance.Should().BeEmpty();
    }

    // (b) only backfilled sessions are ever deleted — a real out-of-target session is never deleted.
    [Fact]
    public void Real_out_of_target_session_is_never_deleted()
    {
        var realL2 = Real(L2);
        var plan = BackfillReconciler.Plan(
            new[] { L1 },
            new[] { Backfilled(L1), realL2 });

        plan.DeleteSessionIds.Should().NotContain(realL2.SessionId);
        plan.DeleteSessionIds.Should().BeEmpty();
    }

    // (c) a real session covering a target lesson is left untouched (no create, no delete).
    [Fact]
    public void Target_lesson_covered_by_real_session_is_left_alone()
    {
        var plan = BackfillReconciler.Plan(
            new[] { L1 },
            new[] { Real(L1) });

        plan.CreateForLessonIds.Should().BeEmpty(); // already covered → don't override real
        plan.DeleteSessionIds.Should().BeEmpty();
    }

    // (d) lower the position → excess backfilled sessions deleted.
    [Fact]
    public void Lowering_position_deletes_excess_backfill()
    {
        var bfL2 = Backfilled(L2);
        var plan = BackfillReconciler.Plan(
            new[] { L1 },                      // was [L1, L2]
            new[] { Backfilled(L1), bfL2 });

        plan.DeleteSessionIds.Should().ContainSingle().Which.Should().Be(bfL2.SessionId);
        plan.CreateForLessonIds.Should().BeEmpty();
    }

    // (e) raise the position → missing backfilled sessions added.
    [Fact]
    public void Raising_position_adds_missing_backfill()
    {
        var plan = BackfillReconciler.Plan(
            new[] { L1, L2, L3 },
            new[] { Backfilled(L1), Backfilled(L2) });

        plan.CreateForLessonIds.Should().ContainSingle().Which.Should().Be(L3);
        plan.DeleteSessionIds.Should().BeEmpty();
    }

    // (f) safety belt — an excess backfilled session WITH attendance is reported, not deleted.
    [Fact]
    public void Backfilled_with_attendance_is_reported_not_deleted()
    {
        var bfWithAtt = Backfilled(L2, hasAttendance: true);
        var plan = BackfillReconciler.Plan(
            new[] { L1 },
            new[] { Backfilled(L1), bfWithAtt });

        plan.DeleteSessionIds.Should().NotContain(bfWithAtt.SessionId);
        plan.SkippedWithAttendance.Should().ContainSingle().Which.Should().Be(bfWithAtt.SessionId);
    }

    // First-time onboarding of a class with no sessions at all → create all target lessons.
    [Fact]
    public void Fresh_class_creates_all_target_lessons()
    {
        var plan = BackfillReconciler.Plan(
            new[] { L1, L2, L3 },
            Array.Empty<BackfillSession>());

        plan.CreateForLessonIds.Should().BeEquivalentTo(new[] { L1, L2, L3 });
        plan.DeleteSessionIds.Should().BeEmpty();
    }
}
