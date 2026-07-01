using System.Text.Json;
using System.Text.Json.Nodes;
using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Exercises;

// ===== DTOs =================================================================

/// <summary>One exercise to add/update in a lesson (bulk-upsert input). Content is the
/// raw type-specific JSON sent by the client.</summary>
public sealed record ExerciseInputDto(string Type, string? Title, int OrderIndex, JsonElement Content);

/// <summary>The current user's saved self-check result for an exercise.</summary>
public sealed record ExerciseResultDto(JsonNode? Answers, int Score, int Total, bool IsCompleted);

/// <summary>An exercise + the current user's result (Result null ⇒ not attempted yet).
/// Content/Answers are emitted as nested JSON (not strings).</summary>
public sealed record ExerciseWithResultDto(
    Guid Id, string Type, string Title, int OrderIndex, JsonNode? Content, ExerciseResultDto? Result);

/// <summary>Outcome of checking a submitted answer.</summary>
public sealed record SubmitResultDto(int Score, int Total);

// ===== Use cases ===========================================================

/// <summary>
/// Bulk add/update exercises for a lesson. Upsert keyed on (LessonId, OrderIndex): an
/// existing slot is UPDATEd, a new one INSERTed. Runs in one transaction. Returns the
/// resulting exercise ids in input order.
/// </summary>
public sealed record AddExercisesToLessonCommand(Guid LessonId, IReadOnlyList<ExerciseInputDto> Exercises)
    : IRequest<Result<IReadOnlyList<Guid>>>;

/// <summary>All exercises for a lesson + the user's previous result, via one LEFT-JOIN query.</summary>
public sealed record GetLessonExercisesQuery(Guid LessonId, Guid UserId)
    : IRequest<Result<IReadOnlyList<ExerciseWithResultDto>>>;

/// <summary>Check a user's answer (ExerciseChecker) and upsert the result (keyed exercise+user).</summary>
public sealed record SubmitExerciseAnswerCommand(Guid LessonExerciseId, Guid UserId, JsonElement Answers)
    : IRequest<Result<SubmitResultDto>>;

// ===== Teacher: student results for a lesson's exercises ====================

public sealed record ExerciseResultsHeaderDto(Guid Id, string Title, string Type, int OrderIndex);

/// <summary>One student's result for one exercise (missing ⇒ not attempted).</summary>
public sealed record StudentExerciseResultDto(Guid ExerciseId, int Score, int Total, bool IsCompleted);

public sealed record StudentExerciseSummaryDto(
    Guid StudentUserId, string StudentName, int CompletedCount, int TotalExercises,
    IReadOnlyList<StudentExerciseResultDto> Results);

/// <summary>A lesson's exercises + every enrolled student's results (teacher view).</summary>
public sealed record LessonExerciseResultsDto(
    Guid LessonId,
    IReadOnlyList<ExerciseResultsHeaderDto> Exercises,
    IReadOnlyList<StudentExerciseSummaryDto> Students);

/// <summary>Teacher/admin: how every student in a class did on a lesson's exercises.</summary>
public sealed record GetLessonExerciseResultsQuery(Guid LessonId, Guid ClassId)
    : IRequest<Result<LessonExerciseResultsDto>>;
