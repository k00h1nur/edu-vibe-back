using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;

namespace LMS.Application.Features.Tasks;

public sealed record LearningTaskDto(
    Guid Id,
    Guid AssignmentId,
    int Order,
    LearningTaskType Type,
    string Title,
    int Points,
    string ContentJson,
    /// <summary>Hidden for students — only included for teachers/admins.</summary>
    string? SolutionJson,
    DateTime CreatedAt);

public sealed record GetAssignmentTasksQuery(Guid AssignmentId, bool IncludeSolutions = false)
    : IRequest<Result<IReadOnlyCollection<LearningTaskDto>>>;

public sealed record GetTaskByIdQuery(Guid TaskId, bool IncludeSolution = false)
    : IRequest<Result<LearningTaskDto>>;

public sealed record CreateTaskCommand(
    Guid AssignmentId,
    int Order,
    LearningTaskType Type,
    string Title,
    int Points,
    string ContentJson,
    string? SolutionJson) : IRequest<Result<LearningTaskDto>>;

public sealed record UpdateTaskCommand(
    Guid TaskId,
    int Order,
    string Title,
    int Points,
    string ContentJson,
    string? SolutionJson) : IRequest<Result<LearningTaskDto>>;

public sealed record DeleteTaskCommand(Guid TaskId) : IRequest<Result>;

public sealed record ReorderTasksCommand(Guid AssignmentId, IReadOnlyList<Guid> TaskIdsInOrder)
    : IRequest<Result>;
