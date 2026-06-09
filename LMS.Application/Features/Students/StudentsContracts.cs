using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Students;

public sealed record StudentDto(
    Guid StudentProfileId,
    Guid UserId,
    string Email,
    int Xp,
    int Streak,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Description,
    string? ParentPhoneNumber,
    string? Level,
    string? AvatarUrl);

public sealed record UpdateStudentAdminFieldsCommand(
    Guid StudentProfileId,
    string? ParentPhoneNumber,
    string? Level) : IRequest<Result<StudentDto>>;

public sealed record SetStudentAvatarCommand(Guid StudentProfileId, string? AvatarUrl)
    : IRequest<Result<StudentDto>>;

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

/// <summary>
/// Updates the editable profile fields (name, phone, description). XP and
/// streak are separate via <see cref="UpdateStudentProfileCommand"/>.
/// </summary>
public sealed record UpdateStudentDetailsCommand(
    Guid StudentProfileId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Description) : IRequest<Result<StudentDto>>;

public sealed record GetStudentsQuery(int Page = 1, int PageSize = 25, string? Search = null)
    : IRequest<Result<PagedResult<StudentDto>>>;

public sealed record GetStudentDetailQuery(Guid StudentProfileId) : IRequest<Result<StudentDto>>;

/// <summary>
/// Returns the student profile linked to the currently authenticated user.
/// Resolved via the <c>studentProfileId</c> JWT claim, falling back to a UserId lookup.
/// </summary>
public sealed record GetMyStudentProfileQuery : IRequest<Result<StudentDto>>;
