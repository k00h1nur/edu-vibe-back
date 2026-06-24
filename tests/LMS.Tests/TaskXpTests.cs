using FluentAssertions;
using LMS.Application.Features.TaskSubmissions;
using LMS.Domain.Entities;
using Xunit;

namespace LMS.Tests;

/// <summary>
/// F4 XP-on-grade: the proportional amount math + the award-once domain invariant
/// (the XpAwarded flag survives re-submission, so XP can't be granted twice).
/// </summary>
public sealed class TaskXpTests
{
    [Theory]
    [InlineData(10, 1.0, 10)]   // full marks
    [InlineData(10, 0.5, 5)]    // half
    [InlineData(10, 0.0, 0)]    // wrong → nothing
    [InlineData(10, 0.33, 3)]   // 3.3 → 3
    [InlineData(10, 0.35, 4)]   // 3.5 → 4 (round half up)
    [InlineData(5, 1.0, 5)]
    [InlineData(10, 1.5, 10)]   // score clamped to 1
    [InlineData(0, 1.0, 0)]     // non-positive points
    [InlineData(-5, 1.0, 0)]
    public void ForGrade_is_proportional_round_half_up(int points, double score, int expected)
        => TaskXp.ForGrade(points, (decimal)score).Should().Be(expected);

    [Fact]
    public void XpAwarded_flag_survives_resubmission()
    {
        var s = new TaskSubmission(Guid.NewGuid(), Guid.NewGuid(), "{}");
        s.Grade(1m, true, gradedByUserId: null, feedback: null);
        s.MarkXpAwarded();
        s.XpAwarded.Should().BeTrue();

        s.UpdateResponse("{\"answer\":\"x\"}"); // re-submit resets grading state…
        s.Score.Should().BeNull();
        s.XpAwarded.Should().BeTrue();           // …but never the award-once flag
    }
}
