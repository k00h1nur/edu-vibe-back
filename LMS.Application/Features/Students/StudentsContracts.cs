using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Students;

public sealed record StudentDto(Guid StudentProfileId, Guid UserId, string Email, int Xp, int Streak);

public sealed record StudentsPingCommand : IRequest<Result<string>>;

public sealed class StudentsPingCommandHandler : IRequestHandler<StudentsPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(StudentsPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Students module ready"));
    }
}

public sealed record RegisterStudentCommand(Guid UserId) : IRequest<Result<StudentDto>>;

public sealed record UpdateStudentProfileCommand(Guid StudentProfileId, int Xp, int Streak)
    : IRequest<Result<StudentDto>>;

public sealed record GetStudentsQuery : IRequest<Result<IReadOnlyCollection<StudentDto>>>;

public sealed record GetStudentDetailQuery(Guid StudentProfileId) : IRequest<Result<StudentDto>>;

/// <summary>
/// Returns the student profile linked to the currently authenticated user.
/// Resolved via the <c>studentProfileId</c> JWT claim, falling back to a UserId lookup.
/// </summary>
public sealed record GetMyStudentProfileQuery : IRequest<Result<StudentDto>>;
