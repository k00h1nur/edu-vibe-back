using FluentAssertions;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;
using Xunit;

namespace LMS.Tests;

/// <summary>
/// Richer submission grading (R/L/W skill tasks): score out of an optional max +
/// feedback, with the back-compatible score-only path and the range guards.
/// </summary>
public sealed class SubmissionGradeTests
{
    private static Submission NewSubmission() =>
        Submission.Create(Guid.NewGuid(), Guid.NewGuid(), "answer", Array.Empty<Submission>());

    [Fact]
    public void Grade_with_max_and_feedback_sets_all_fields()
    {
        var s = NewSubmission();
        s.Grade(8m, 10m, "  Great work  ");
        s.Score.Should().Be(8m);
        s.MaxScore.Should().Be(10m);
        s.Feedback.Should().Be("Great work");     // trimmed
        s.Status.Should().Be(SubmissionStatus.Graded);
    }

    [Fact]
    public void Grade_score_only_stays_back_compatible()
    {
        var s = NewSubmission();
        s.Grade(5m);
        s.Score.Should().Be(5m);
        s.MaxScore.Should().BeNull();
        s.Feedback.Should().BeNull();
        s.Status.Should().Be(SubmissionStatus.Graded);
    }

    [Theory]
    [InlineData(11, 10)]   // score above max
    [InlineData(5, 0)]     // non-positive max
    public void Grade_rejects_out_of_range(double score, double max)
    {
        var s = NewSubmission();
        var act = () => s.Grade((decimal)score, (decimal)max, null);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Grade_rejects_negative_score()
    {
        var s = NewSubmission();
        var act = () => s.Grade(-1m);
        act.Should().Throw<DomainException>();
    }
}
