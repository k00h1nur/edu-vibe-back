using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Assignments;

public sealed record AssignmentDto(
    Guid Id,
    Guid ClassId,
    string Title,
    AssignmentStatus Status,
    Guid CreatedByTeacherId);

public sealed record AssignmentsPingCommand : IRequest<Result<string>>;

public sealed class AssignmentsPingCommandHandler : IRequestHandler<AssignmentsPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(AssignmentsPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Assignments module ready"));
    }
}

public sealed record CreateAssignmentCommand(Guid ClassId, string Title, Guid TeacherUserId)
    : IRequest<Result<AssignmentDto>>;

public sealed record UpdateAssignmentCommand(Guid AssignmentId, string Title) : IRequest<Result<AssignmentDto>>;

public sealed record PublishAssignmentCommand(Guid AssignmentId) : IRequest<Result<AssignmentDto>>;

public sealed record CloseAssignmentCommand(Guid AssignmentId) : IRequest<Result<AssignmentDto>>;

public sealed record GetClassAssignmentsQuery(Guid ClassId) : IRequest<Result<IReadOnlyCollection<AssignmentDto>>>;

public sealed record GetStudentAssignmentsQuery(Guid StudentProfileId)
    : IRequest<Result<IReadOnlyCollection<AssignmentDto>>>;