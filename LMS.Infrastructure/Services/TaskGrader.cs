using System.Text.Json;
using LMS.Application.Common.Abstractions;
using LMS.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Default <see cref="ITaskGrader"/> — pattern-matches on
/// <see cref="LearningTaskType"/> and compares the response JSON shape to the
/// solution JSON shape stored on the task. All JSON parsing is forgiving:
/// on any malformed shape the grader degrades to manual review rather than
/// crashing the request.
/// </summary>
public sealed class TaskGrader(ILogger<TaskGrader> logger) : ITaskGrader
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public GradeResult Grade(LearningTask task, string responseJson)
    {
        if (string.IsNullOrWhiteSpace(task.SolutionJson)) return GradeResult.RequiresManualReview;

        try
        {
            return task.Type switch
            {
                LearningTaskType.MultipleChoice => GradeMultipleChoice(task.SolutionJson, responseJson),
                LearningTaskType.FillGaps => GradeFillGaps(task.SolutionJson, responseJson),
                LearningTaskType.ShortAnswer => GradeShortAnswer(task.SolutionJson, responseJson),
                LearningTaskType.Matching => GradeMatching(task.SolutionJson, responseJson),
                LearningTaskType.Ordering => GradeOrdering(task.SolutionJson, responseJson),
                LearningTaskType.Listening => GradeListening(task, responseJson),
                LearningTaskType.Test => GradeResult.RequiresManualReview, // composite — grade per child task
                _ => GradeResult.RequiresManualReview,
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex,
                "Auto-grading failed for task {TaskId} of type {Type} — falling back to manual review.",
                task.Id, task.Type);
            return GradeResult.RequiresManualReview;
        }
    }

    private static GradeResult GradeMultipleChoice(string solutionJson, string responseJson)
    {
        var solution = JsonSerializer.Deserialize<MultipleChoiceSolution>(solutionJson, Json);
        var response = JsonSerializer.Deserialize<MultipleChoiceResponse>(responseJson, Json);
        if (solution?.CorrectIndices is null || response?.SelectedIndices is null)
            return GradeResult.RequiresManualReview;

        var correct = new HashSet<int>(solution.CorrectIndices);
        var picked = new HashSet<int>(response.SelectedIndices);
        return correct.SetEquals(picked) ? GradeResult.Correct : GradeResult.Incorrect;
    }

    private static GradeResult GradeFillGaps(string solutionJson, string responseJson)
    {
        var solution = JsonSerializer.Deserialize<FillGapsSolution>(solutionJson, Json);
        var response = JsonSerializer.Deserialize<FillGapsResponse>(responseJson, Json);
        if (solution?.Fills is null || response?.Fills is null) return GradeResult.RequiresManualReview;
        if (solution.Fills.Count == 0) return GradeResult.RequiresManualReview;

        var matches = 0;
        var len = Math.Min(solution.Fills.Count, response.Fills.Count);
        for (var i = 0; i < len; i++)
        {
            if (string.Equals(
                    solution.Fills[i]?.Trim(),
                    response.Fills[i]?.Trim(),
                    StringComparison.OrdinalIgnoreCase))
            {
                matches++;
            }
        }

        var score = (decimal)matches / solution.Fills.Count;
        return GradeResult.Partial(score);
    }

    private static GradeResult GradeShortAnswer(string solutionJson, string responseJson)
    {
        var solution = JsonSerializer.Deserialize<ShortAnswerSolution>(solutionJson, Json);
        var response = JsonSerializer.Deserialize<ShortAnswerResponse>(responseJson, Json);
        if (solution?.AcceptedAnswers is null || response?.Answer is null) return GradeResult.RequiresManualReview;

        var comparer = solution.CaseSensitive == true
            ? StringComparer.Ordinal
            : StringComparer.OrdinalIgnoreCase;
        var answer = response.Answer.Trim();
        var hit = solution.AcceptedAnswers.Any(a =>
            !string.IsNullOrWhiteSpace(a) && comparer.Equals(a.Trim(), answer));
        return hit ? GradeResult.Correct : GradeResult.Incorrect;
    }

    private static GradeResult GradeMatching(string solutionJson, string responseJson)
    {
        var solution = JsonSerializer.Deserialize<MatchingSolution>(solutionJson, Json);
        var response = JsonSerializer.Deserialize<MatchingResponse>(responseJson, Json);
        if (solution?.Pairs is null || response?.Pairs is null) return GradeResult.RequiresManualReview;
        if (solution.Pairs.Count == 0) return GradeResult.RequiresManualReview;

        var solutionSet = solution.Pairs
            .Where(p => p.Count == 2)
            .Select(p => (p[0], p[1]))
            .ToHashSet();
        var responseSet = response.Pairs
            .Where(p => p.Count == 2)
            .Select(p => (p[0], p[1]))
            .ToHashSet();

        var correctMatches = solutionSet.Intersect(responseSet).Count();
        var score = (decimal)correctMatches / solution.Pairs.Count;
        return GradeResult.Partial(score);
    }

    private static GradeResult GradeOrdering(string solutionJson, string responseJson)
    {
        var solution = JsonSerializer.Deserialize<OrderingSolution>(solutionJson, Json);
        var response = JsonSerializer.Deserialize<OrderingResponse>(responseJson, Json);
        if (solution?.Order is null || response?.Order is null) return GradeResult.RequiresManualReview;

        return solution.Order.SequenceEqual(response.Order)
            ? GradeResult.Correct
            : GradeResult.Incorrect;
    }

    private static GradeResult GradeListening(LearningTask task, string responseJson)
    {
        // Listening content carries a `format` discriminator. If it's
        // "multiple-choice", the solution shape matches MultipleChoiceSolution.
        // If "short-answer", it matches ShortAnswerSolution. Free-text without
        // a structured solution → manual review.
        try
        {
            using var content = JsonDocument.Parse(task.ContentJson);
            var format = content.RootElement.TryGetProperty("format", out var f)
                ? f.GetString()?.ToLowerInvariant()
                : null;
            return format switch
            {
                "multiple-choice" => GradeMultipleChoice(task.SolutionJson!, responseJson),
                "short-answer" => GradeShortAnswer(task.SolutionJson!, responseJson),
                _ => GradeResult.RequiresManualReview,
            };
        }
        catch
        {
            return GradeResult.RequiresManualReview;
        }
    }

    // ---- JSON shapes -------------------------------------------------------

    private sealed record MultipleChoiceSolution(List<int> CorrectIndices);
    private sealed record MultipleChoiceResponse(List<int> SelectedIndices);

    private sealed record FillGapsSolution(List<string> Fills);
    private sealed record FillGapsResponse(List<string> Fills);

    private sealed record ShortAnswerSolution(List<string> AcceptedAnswers, bool? CaseSensitive);
    private sealed record ShortAnswerResponse(string Answer);

    private sealed record MatchingSolution(List<List<int>> Pairs);
    private sealed record MatchingResponse(List<List<int>> Pairs);

    private sealed record OrderingSolution(List<int> Order);
    private sealed record OrderingResponse(List<int> Order);
}
