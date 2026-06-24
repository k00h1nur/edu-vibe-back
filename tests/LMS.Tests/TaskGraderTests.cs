using FluentAssertions;
using LMS.Application.Common.Abstractions;
using LMS.Domain.Entities;
using LMS.Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LMS.Tests;

/// <summary>
/// Auto-grader coverage: each gradeable type, the manual-review fallthrough, and
/// (F4) FillGaps per-blank variants — including a backward-compat proof that
/// single-string-blank content still grades exactly as before.
/// </summary>
public sealed class TaskGraderTests
{
    private readonly TaskGrader _grader = new(NullLogger<TaskGrader>.Instance);

    private static LearningTask Task(LearningTaskType type, string? solutionJson, string contentJson = "{}")
        => new(Guid.NewGuid(), 1, type, "task", 10, contentJson, solutionJson);

    private GradeResult Grade(LearningTaskType type, string? solution, string response, string content = "{}")
        => _grader.Grade(Task(type, solution, content), response);

    [Fact]
    public void MultipleChoice_exact_set_is_correct()
    {
        var r = Grade(LearningTaskType.MultipleChoice, "{\"correctIndices\":[0,2]}", "{\"selectedIndices\":[2,0]}");
        r.AutoGraded.Should().BeTrue();
        r.IsCorrect.Should().BeTrue();
        r.Score.Should().Be(1m);
    }

    [Fact]
    public void MultipleChoice_wrong_set_is_incorrect()
        => Grade(LearningTaskType.MultipleChoice, "{\"correctIndices\":[0,2]}", "{\"selectedIndices\":[0]}")
            .IsCorrect.Should().BeFalse();

    // Backward-compat proof: single-string blanks grade exactly as before.
    [Fact]
    public void FillGaps_single_string_blanks_still_grade()
        => Grade(LearningTaskType.FillGaps, "{\"fills\":[\"is\",\"are\"]}", "{\"fills\":[\"is\",\"are\"]}")
            .Score.Should().Be(1m);

    [Fact]
    public void FillGaps_is_case_insensitive_and_trimmed()
        => Grade(LearningTaskType.FillGaps, "{\"fills\":[\"is\"]}", "{\"fills\":[\"  IS \"]}")
            .Score.Should().Be(1m);

    [Fact]
    public void FillGaps_gives_partial_credit()
        => Grade(LearningTaskType.FillGaps, "{\"fills\":[\"is\",\"are\"]}", "{\"fills\":[\"is\",\"x\"]}")
            .Score.Should().Be(0.5m);

    // F4: per-blank variants. Canonical still accepted; variant accepted; wrong rejected.
    [Fact]
    public void FillGaps_accepts_per_blank_variant()
    {
        const string sol = "{\"fills\":[\"color\"],\"acceptedVariants\":[[\"colour\"]]}";
        Grade(LearningTaskType.FillGaps, sol, "{\"fills\":[\"colour\"]}").Score.Should().Be(1m);
        Grade(LearningTaskType.FillGaps, sol, "{\"fills\":[\"COLOR\"]}").Score.Should().Be(1m);
        Grade(LearningTaskType.FillGaps, sol, "{\"fills\":[\"nope\"]}").Score.Should().Be(0m);
    }

    [Fact]
    public void FillGaps_variants_align_by_index()
    {
        // blank 0 accepts "I am"/"I'm"; blank 1 has no extra variants.
        const string sol = "{\"fills\":[\"I am\",\"are\"],\"acceptedVariants\":[[\"I'm\"],[]]}";
        Grade(LearningTaskType.FillGaps, sol, "{\"fills\":[\"I'm\",\"are\"]}").Score.Should().Be(1m);
    }

    [Fact]
    public void ShortAnswer_accepts_any_listed_answer_case_insensitive_by_default()
    {
        const string sol = "{\"acceptedAnswers\":[\"yes\",\"yeah\"]}";
        Grade(LearningTaskType.ShortAnswer, sol, "{\"answer\":\"YES\"}").IsCorrect.Should().BeTrue();
        Grade(LearningTaskType.ShortAnswer, sol, "{\"answer\":\"nope\"}").IsCorrect.Should().BeFalse();
    }

    [Fact]
    public void ShortAnswer_respects_case_sensitive_flag()
    {
        const string sol = "{\"acceptedAnswers\":[\"Paris\"],\"caseSensitive\":true}";
        Grade(LearningTaskType.ShortAnswer, sol, "{\"answer\":\"Paris\"}").IsCorrect.Should().BeTrue();
        Grade(LearningTaskType.ShortAnswer, sol, "{\"answer\":\"paris\"}").IsCorrect.Should().BeFalse();
    }

    [Fact]
    public void Matching_scores_by_correct_pairs()
    {
        const string sol = "{\"pairs\":[[0,1],[1,0]]}";
        Grade(LearningTaskType.Matching, sol, "{\"pairs\":[[0,1],[1,0]]}").Score.Should().Be(1m);
        Grade(LearningTaskType.Matching, sol, "{\"pairs\":[[0,1],[1,2]]}").Score.Should().Be(0.5m);
    }

    [Fact]
    public void Ordering_exact_sequence_is_correct()
    {
        const string sol = "{\"order\":[2,0,1]}";
        Grade(LearningTaskType.Ordering, sol, "{\"order\":[2,0,1]}").IsCorrect.Should().BeTrue();
        Grade(LearningTaskType.Ordering, sol, "{\"order\":[0,1,2]}").IsCorrect.Should().BeFalse();
    }

    [Fact]
    public void Test_type_requires_manual_review()
        => Grade(LearningTaskType.Test, "{}", "{}").AutoGraded.Should().BeFalse();

    [Fact]
    public void Missing_solution_requires_manual_review()
        => _grader.Grade(Task(LearningTaskType.MultipleChoice, solutionJson: null), "{\"selectedIndices\":[0]}")
            .AutoGraded.Should().BeFalse();

    [Fact]
    public void Listening_freetext_requires_manual_review()
        => Grade(LearningTaskType.Listening, "{}", "{}", content: "{\"format\":\"free-text\"}")
            .AutoGraded.Should().BeFalse();

    [Fact]
    public void Malformed_response_falls_back_to_manual_review()
        => Grade(LearningTaskType.MultipleChoice, "{\"correctIndices\":[0]}", "not json")
            .AutoGraded.Should().BeFalse();
}
