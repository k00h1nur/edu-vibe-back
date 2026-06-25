using FluentAssertions;
using LMS.Application.Common;
using LMS.Application.Features.Sessions;
using LMS.Application.Features.Tasks;
using Xunit;

namespace LMS.Tests;

/// <summary>
/// Pure-core coverage for the auto-materialise + date-gate feature:
///   • SchoolCalendar — the single shared lesson-day / visibility rule (UTC+5).
///   • LessonTaskMaterialization — re-run idempotency (no duplicate tasks).
///   • SchedulePreserve — homework'd sessions survive a reschedule (no orphan/dup).
/// </summary>
public sealed class AutoMaterializeDateGateTests
{
    // ---- SchoolCalendar: school-local "today" (Tashkent UTC+5) ----------------
    [Fact]
    public void Today_rolls_over_at_local_midnight_not_utc_midnight()
    {
        // 2026-06-24 19:00 UTC == 2026-06-25 00:00 Tashkent → school day is the 25th.
        SchoolCalendar.Today(new DateTime(2026, 6, 24, 19, 0, 0, DateTimeKind.Utc))
            .Should().Be(new DateOnly(2026, 6, 25));
        // One minute earlier (23:59 local on the 24th) → still the 24th.
        SchoolCalendar.Today(new DateTime(2026, 6, 24, 18, 59, 0, DateTimeKind.Utc))
            .Should().Be(new DateOnly(2026, 6, 24));
    }

    // ---- SchoolCalendar: visibility rule (before / on / after / null) ---------
    [Fact]
    public void Homework_hidden_before_the_lesson_day()
    {
        var today = new DateOnly(2026, 6, 25);
        SchoolCalendar.IsLessonHomeworkVisibleToStudent(new DateOnly(2026, 6, 26), today).Should().BeFalse();
    }

    [Fact]
    public void Homework_visible_on_the_lesson_day()
    {
        var today = new DateOnly(2026, 6, 25);
        SchoolCalendar.IsLessonHomeworkVisibleToStudent(new DateOnly(2026, 6, 25), today).Should().BeTrue();
    }

    [Fact]
    public void Homework_visible_after_the_lesson_day()
    {
        var today = new DateOnly(2026, 6, 25);
        SchoolCalendar.IsLessonHomeworkVisibleToStudent(new DateOnly(2026, 6, 24), today).Should().BeTrue();
    }

    [Fact]
    public void Homework_with_no_lesson_date_is_always_visible()
        => SchoolCalendar.IsLessonHomeworkVisibleToStudent(null, new DateOnly(2026, 1, 1)).Should().BeTrue();

    [Fact]
    public void Unlocks_exactly_at_local_midnight_of_the_lesson_day()
    {
        var lesson = new DateOnly(2026, 6, 25);
        // 18:59 UTC on the 24th = 23:59 Tashkent on the 24th → still hidden.
        var justBefore = SchoolCalendar.Today(new DateTime(2026, 6, 24, 18, 59, 0, DateTimeKind.Utc));
        SchoolCalendar.IsLessonHomeworkVisibleToStudent(lesson, justBefore).Should().BeFalse();
        // 19:00 UTC on the 24th = 00:00 Tashkent on the 25th → unlocks.
        var atMidnight = SchoolCalendar.Today(new DateTime(2026, 6, 24, 19, 0, 0, DateTimeKind.Utc));
        SchoolCalendar.IsLessonHomeworkVisibleToStudent(lesson, atMidnight).Should().BeTrue();
    }

    // ---- LessonTaskMaterialization: idempotency (no duplicate tasks) ----------
    [Fact]
    public void First_materialize_creates_every_blueprint()
        => LessonTaskMaterialization.OrdersToCreate(new[] { 1, 2, 3 }, new HashSet<int>())
            .Should().Equal(1, 2, 3);

    [Fact]
    public void Rerun_materialize_creates_nothing()
        => LessonTaskMaterialization.OrdersToCreate(new[] { 1, 2, 3 }, new HashSet<int> { 1, 2, 3 })
            .Should().BeEmpty();

    [Fact]
    public void Materialize_adds_only_missing_orders()
        => LessonTaskMaterialization.OrdersToCreate(new[] { 1, 2, 3 }, new HashSet<int> { 1 })
            .Should().Equal(2, 3);

    [Fact]
    public void Materialize_dedupes_repeated_blueprint_orders()
        => LessonTaskMaterialization.OrdersToCreate(new[] { 1, 1, 2 }, new HashSet<int>())
            .Should().Equal(1, 2);

    [Fact]
    public void Empty_lesson_materializes_nothing()
        => LessonTaskMaterialization.OrdersToCreate(Array.Empty<int>(), new HashSet<int>())
            .Should().BeEmpty();

    // ---- SchedulePreserve: reschedule keeps homework'd / attendance'd sessions -
    [Fact]
    public void Reschedule_preserves_protected_sessions_and_removes_the_rest()
    {
        var plain = new ScheduleSessionRef(Guid.NewGuid(), new DateOnly(2026, 7, 1));
        var withAttendance = new ScheduleSessionRef(Guid.NewGuid(), new DateOnly(2026, 7, 2));
        var withHomework = new ScheduleSessionRef(Guid.NewGuid(), new DateOnly(2026, 7, 3));
        var future = new[] { plain, withAttendance, withHomework };
        var protectedIds = new HashSet<Guid> { withAttendance.Id, withHomework.Id };

        var plan = SchedulePreserve.Partition(future, protectedIds);

        // Only the plain session is deletable; protected ones survive (stable Id) —
        // so their materialised tasks + submissions can never be orphaned or doubled.
        plan.RemovableIds.Should().ContainSingle().Which.Should().Be(plain.Id);
        plan.RemovableIds.Should().NotContain(withHomework.Id);
        // Protected sessions' dates stay occupied so regenerate can't create a duplicate.
        plan.OccupiedDates.Should().BeEquivalentTo(new[] { withAttendance.Date, withHomework.Date });
    }

    [Fact]
    public void Reschedule_with_nothing_protected_removes_all_future_and_occupies_none()
    {
        var future = new[]
        {
            new ScheduleSessionRef(Guid.NewGuid(), new DateOnly(2026, 7, 1)),
            new ScheduleSessionRef(Guid.NewGuid(), new DateOnly(2026, 7, 2)),
        };
        var plan = SchedulePreserve.Partition(future, new HashSet<Guid>());
        plan.RemovableIds.Should().HaveCount(2);
        plan.OccupiedDates.Should().BeEmpty();
    }

    // ---- Composition: a generate re-run is a clean no-op -----------------------
    [Fact]
    public void Generate_rerun_neither_duplicates_nor_orphans_a_materialized_session()
    {
        // A future session that already has materialised homework...
        var homework = new ScheduleSessionRef(Guid.NewGuid(), new DateOnly(2026, 7, 3));
        var protectedIds = new HashSet<Guid> { homework.Id };

        // ...is preserved by the reschedule (not deleted → assignment never orphaned)...
        var plan = SchedulePreserve.Partition(new[] { homework }, protectedIds);
        plan.RemovableIds.Should().BeEmpty();

        // ...and re-materialising it creates zero new tasks (idempotent) — no duplicates.
        LessonTaskMaterialization.OrdersToCreate(new[] { 1, 2 }, new HashSet<int> { 1, 2 })
            .Should().BeEmpty();
    }
}
