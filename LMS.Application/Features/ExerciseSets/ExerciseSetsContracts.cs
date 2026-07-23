using LMS.Application.Common.Models;
using LMS.Application.Features.Exercises; // reuse ExerciseInputDto / ExerciseWithResultDto
using MediatR;

namespace LMS.Application.Features.ExerciseSets;

// ===== DTOs =================================================================

/// <summary>A reusable exercise set as its owner/manager sees it: title, how many
/// exercises it holds, and which classes it's attached to.</summary>
public sealed record ExerciseSetDto(
    Guid Id, string Title, string? Description, Guid CreatedByUserId,
    int ExerciseCount, IReadOnlyList<Guid> ClassIds, DateTime CreatedAt);

/// <summary>A set as a student sees it in their list — with their own progress.</summary>
public sealed record StudentExerciseSetDto(
    Guid Id, string Title, string? Description, int ExerciseCount, int CompletedCount);

// ===== Use cases ===========================================================

public sealed record CreateExerciseSetCommand(string Title, string? Description, Guid CreatedByUserId)
    : IRequest<Result<ExerciseSetDto>>;

public sealed record UpdateExerciseSetCommand(Guid SetId, string Title, string? Description)
    : IRequest<Result<ExerciseSetDto>>;

public sealed record DeleteExerciseSetCommand(Guid SetId) : IRequest<Result>;

/// <summary>Sets the caller manages — their own, or ALL for an admin.</summary>
public sealed record GetExerciseSetsQuery() : IRequest<Result<IReadOnlyList<ExerciseSetDto>>>;

public sealed record GetExerciseSetByIdQuery(Guid SetId) : IRequest<Result<ExerciseSetDto>>;

/// <summary>Replace the set's attached classes wholesale (like the materials class picker).</summary>
public sealed record SetExerciseSetClassesCommand(Guid SetId, IReadOnlyList<Guid> ClassIds) : IRequest<Result>;

/// <summary>Bulk add/update the set's exercises — upsert by (ExerciseSetId, OrderIndex),
/// reusing the same <see cref="ExerciseInputDto"/> the lesson authoring dialog sends.</summary>
public sealed record AddExercisesToSetCommand(Guid SetId, IReadOnlyList<ExerciseInputDto> Exercises)
    : IRequest<Result<IReadOnlyList<Guid>>>;

/// <summary>A set's exercises + the caller's own saved results (reuses
/// <see cref="ExerciseWithResultDto"/>). Access-checked: admin, the set owner, a teacher of
/// an attached class, or a student enrolled in one.</summary>
public sealed record GetSetExercisesQuery(Guid SetId) : IRequest<Result<IReadOnlyList<ExerciseWithResultDto>>>;

/// <summary>The sets reachable to the current student (via their enrolled classes) + progress.</summary>
public sealed record GetStudentExerciseSetsQuery() : IRequest<Result<IReadOnlyList<StudentExerciseSetDto>>>;
