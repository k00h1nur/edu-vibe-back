using System;
using FluentAssertions;
using LMS.Domain.Entities;
using LMS.Domain.Exceptions;
using Xunit;

namespace LMS.Tests;

/// <summary>
/// Coverage for teacher grading on <see cref="LessonExerciseSubmission"/> — the path
/// behind grading open-ended (writing) submissions: score-out-of-max guards, feedback
/// trimming, and the rule that a fresh <see cref="LessonExerciseSubmission.Apply"/>
/// (student re-submitted) clears any earlier grade.
/// </summary>
public sealed class LessonExerciseSubmissionTests
{
    private static LessonExerciseSubmission NewSubmission()
        => new(Guid.NewGuid(), Guid.NewGuid(), "{\"text\":\"my essay\"}", 0, 0);

    [Fact]
    public void Grade_sets_score_feedback_and_grader()
    {
        var sub = NewSubmission();
        var grader = Guid.NewGuid();

        sub.Grade(8m, 10m, "  Good structure  ", grader);

        sub.TeacherScore.Should().Be(8m);
        sub.TeacherMaxScore.Should().Be(10m);
        sub.TeacherFeedback.Should().Be("Good structure"); // trimmed
        sub.GradedByUserId.Should().Be(grader);
        sub.GradedAt.Should().NotBeNull();
        sub.IsTeacherGraded.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, 10)]    // minimum
    [InlineData(6.5, 9)]   // band, fractional
    [InlineData(10, 10)]   // maximum
    public void Grade_accepts_scores_within_range(double score, double max)
    {
        var act = () => NewSubmission().Grade((decimal)score, (decimal)max, null, Guid.NewGuid());
        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(-1, 10)]   // below zero
    [InlineData(11, 10)]   // above max
    [InlineData(5, 0)]     // non-positive max
    public void Grade_rejects_out_of_range(double score, double max)
    {
        var act = () => NewSubmission().Grade((decimal)score, (decimal)max, null, Guid.NewGuid());
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Resubmitting_clears_a_previous_teacher_grade()
    {
        var sub = NewSubmission();
        sub.Grade(8m, 10m, "Nice", Guid.NewGuid());
        sub.IsTeacherGraded.Should().BeTrue();

        sub.Apply("{\"text\":\"edited essay\"}", 0, 0);

        sub.IsTeacherGraded.Should().BeFalse();
        sub.TeacherScore.Should().BeNull();
        sub.TeacherMaxScore.Should().BeNull();
        sub.TeacherFeedback.Should().BeNull();
        sub.GradedByUserId.Should().BeNull();
        sub.GradedAt.Should().BeNull();
    }
}
