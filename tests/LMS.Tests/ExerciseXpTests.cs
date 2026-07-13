using FluentAssertions;
using LMS.Application.Common;
using LMS.Application.Features.Exercises;
using LMS.Domain.Entities;
using Xunit;

namespace LMS.Tests;

/// <summary>
/// Phase-1 game rewards for self-check exercises:
///   • ExerciseXp.ForCompletion — 2/slot, floored 5, capped 40;
///   • the award-once invariant (XpAwarded survives a re-submit / Apply);
///   • StudentProfile.RegisterDailyActivity — consecutive-day streak math.
/// </summary>
public sealed class ExerciseXpTests
{
    [Theory]
    [InlineData(0, 0)]    // nothing gradable → no award
    [InlineData(-3, 0)]   // guard
    [InlineData(1, 5)]    // 2 → floored to 5
    [InlineData(2, 5)]    // 4 → floored to 5
    [InlineData(3, 6)]
    [InlineData(5, 10)]
    [InlineData(10, 20)]
    [InlineData(20, 40)]  // exactly the cap
    [InlineData(25, 40)]  // above cap → clamped
    public void ForCompletion_is_two_per_slot_clamped_5_to_40(int total, int expected)
        => ExerciseXp.ForCompletion(total).Should().Be(expected);

    [Fact]
    public void XpAwarded_flag_survives_resubmission()
    {
        var s = new LessonExerciseSubmission(Guid.NewGuid(), Guid.NewGuid(), "{}", 3, 3);
        s.XpAwarded.Should().BeFalse();

        s.MarkXpAwarded();
        s.XpAwarded.Should().BeTrue();

        s.Apply("{\"a\":1}", 2, 3);   // student edited + re-sent
        s.Score.Should().Be(2);        // result updates…
        s.XpAwarded.Should().BeTrue();  // …but the award-once flag is sticky
    }

    [Fact]
    public void RegisterDailyActivity_counts_consecutive_days()
    {
        var user = new User("streak@example.com", "hash");
        var sp = new StudentProfile(user.Id, user);
        sp.Streak.Should().Be(0);

        var day1 = new DateOnly(2026, 7, 13);
        sp.RegisterDailyActivity(day1);
        sp.Streak.Should().Be(1);           // first ever

        sp.RegisterDailyActivity(day1);
        sp.Streak.Should().Be(1);           // same day → no double count

        sp.RegisterDailyActivity(day1.AddDays(1));
        sp.Streak.Should().Be(2);           // next day → +1

        sp.RegisterDailyActivity(day1.AddDays(2));
        sp.Streak.Should().Be(3);           // still consecutive

        sp.RegisterDailyActivity(day1.AddDays(5));
        sp.Streak.Should().Be(1);           // 3-day gap → reset
    }

    [Theory]
    [InlineData(0, 1)]     // floor
    [InlineData(49, 1)]
    [InlineData(50, 2)]    // level 2 @ 50
    [InlineData(199, 2)]
    [InlineData(200, 3)]   // level 3 @ 200
    [InlineData(450, 4)]   // level 4 @ 450
    [InlineData(800, 5)]
    public void LevelFor_matches_the_50x_squared_curve(int xp, int expectedLevel)
        => LevelCurve.LevelFor(xp).Should().Be(expectedLevel);

    [Fact]
    public void Progress_reports_into_level_and_span()
    {
        // 120 XP → level 2 (floor 50, next 200): 70 into a 150-wide level.
        var (level, into, span) = LevelCurve.Progress(120);
        level.Should().Be(2);
        into.Should().Be(70);
        span.Should().Be(150);
    }
}
