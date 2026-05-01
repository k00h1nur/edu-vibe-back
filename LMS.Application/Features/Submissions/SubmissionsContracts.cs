using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Submissions;

public sealed record SubmissionDto(
    Guid Id,
    Guid AssignmentId,
    Guid StudentProfileId,
    string Content,
    SubmissionStatus Status,
    decimal? Score);

public sealed record SubmissionsPingCommand : IRequest<Result<string>>;

public sealed class SubmissionsPingCommandHandler : IRequestHandler<SubmissionsPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(SubmissionsPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Submissions module ready"));
    }
}

public sealed record SubmitAssignmentCommand(Guid AssignmentId, Guid StudentProfileId, string Content, bool IsLate)
    : IRequest<Result<SubmissionDto>>;

public sealed record GradeSubmissionCommand(Guid SubmissionId, decimal Score) : IRequest<Result<SubmissionDto>>;

public sealed record GetAssignmentSubmissionsQuery(Guid AssignmentId)
    : IRequest<Result<IReadOnlyCollection<SubmissionDto>>>;

public sealed record GetStudentSubmissionsQuery(Guid StudentProfileId)
    : IRequest<Result<IReadOnlyCollection<SubmissionDto>>>;