using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;

namespace LMS.Application.Features.TaskSubmissions;

public sealed record TaskSubmissionDto(
    Guid Id,
    Guid TaskId,
    Guid StudentProfileId,
    string ResponseJson,
    decimal? Score,
    bool? IsCorrect,
    TaskSubmissionStatus Status,
    string? TeacherFeedback,
    DateTime CreatedAt,
    DateTime? GradedAt);

/// <summary>
/// Student submits (or re-submits) a response to a task. Auto-gradable types
/// get a verdict on the same roundtrip; manually-graded types come back at
/// <see cref="TaskSubmissionStatus.AwaitingReview"/>.
/// </summary>
public sealed record SubmitTaskResponseCommand(
    Guid TaskId,
    Guid StudentProfileId,
    string ResponseJson) : IRequest<Result<TaskSubmissionDto>>;

/// <summary>Teacher manually grades a submission (open-ended types).</summary>
public sealed record GradeTaskSubmissionCommand(
    Guid SubmissionId,
    decimal Score,
    string? Feedback) : IRequest<Result<TaskSubmissionDto>>;

public sealed record GetTaskSubmissionsByTaskQuery(Guid TaskId)
    : IRequest<Result<IReadOnlyCollection<TaskSubmissionDto>>>;

public sealed record GetMyTaskSubmissionsByAssignmentQuery(Guid AssignmentId, Guid StudentProfileId)
    : IRequest<Result<IReadOnlyCollection<TaskSubmissionDto>>>;
