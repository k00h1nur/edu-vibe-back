using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Assignments;

public sealed record AssignmentDto(
    Guid Id,
    Guid ClassId,
    string Title,
    AssignmentStatus Status,
    Guid CreatedByTeacherId,
    DateTime? DueDate);

public sealed record AssignmentsPingCommand : IRequest<Result<string>>;

public sealed class AssignmentsPingCommandHandler : IRequestHandler<AssignmentsPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(AssignmentsPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Assignments module ready"));
    }
}

public sealed record CreateAssignmentCommand(Guid ClassId, string Title, Guid TeacherUserId, DateTime? DueDate = null)
    : IRequest<Result<AssignmentDto>>;

public sealed record UpdateAssignmentCommand(Guid AssignmentId, string Title, DateTime? DueDate = null)
    : IRequest<Result<AssignmentDto>>;

public sealed record PublishAssignmentCommand(Guid AssignmentId) : IRequest<Result<AssignmentDto>>;

public sealed record CloseAssignmentCommand(Guid AssignmentId) : IRequest<Result<AssignmentDto>>;

public sealed record GetClassAssignmentsQuery(Guid ClassId) : IRequest<Result<IReadOnlyCollection<AssignmentDto>>>;

public sealed record GetStudentAssignmentsQuery(Guid StudentProfileId)
    : IRequest<Result<IReadOnlyCollection<AssignmentDto>>>;

/// <summary>
/// Lists assignments, optionally filtered. Used by the teacher Homework page and admin views.
/// </summary>
public sealed record GetAssignmentsQuery(
    Guid? TeacherUserId = null,
    Guid? ClassId = null,
    AssignmentStatus? Status = null)
    : IRequest<Result<IReadOnlyCollection<AssignmentDto>>>;

// ----- Book attachments ---------------------------------------------------

public sealed record AssignmentBookDto(Guid Id, Guid BookId, string? Note);

public sealed record AttachBookToAssignmentCommand(Guid AssignmentId, Guid BookId, string? Note)
    : IRequest<Result<AssignmentBookDto>>;

public sealed record DetachBookFromAssignmentCommand(Guid AssignmentId, Guid BookId)
    : IRequest<Result>;

public sealed record GetAssignmentBooksQuery(Guid AssignmentId)
    : IRequest<Result<IReadOnlyCollection<AssignmentBookDto>>>;

// ----- Per-student targeting ---------------------------------------------

public sealed record AssignmentAssigneeDto(Guid AssignmentId, Guid StudentProfileId);

/// <summary>
/// Replace the assignee set on an assignment. Empty list = whole class
/// (the implicit default). Use for "assign to selected students" workflows.
/// </summary>
public sealed record SetAssignmentAssigneesCommand(
    Guid AssignmentId,
    IReadOnlyCollection<Guid> StudentProfileIds)
    : IRequest<Result<IReadOnlyCollection<AssignmentAssigneeDto>>>;

public sealed record GetAssignmentAssigneesQuery(Guid AssignmentId)
    : IRequest<Result<IReadOnlyCollection<AssignmentAssigneeDto>>>;