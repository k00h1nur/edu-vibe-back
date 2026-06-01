using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Classes;

public sealed record ClassDto(
    Guid Id,
    string Title,
    int MaxStudents,
    Modality Modality,
    ClassStatus Status,
    Guid? TeacherUserId);

public sealed record ClassesPingCommand : IRequest<Result<string>>;

public sealed class ClassesPingCommandHandler : IRequestHandler<ClassesPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(ClassesPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Classes module ready"));
    }
}

public sealed record CreateClassCommand(string Title, int MaxStudents, Modality Modality, Guid? TeacherUserId)
    : IRequest<Result<ClassDto>>;

public sealed record UpdateClassCommand(
    Guid ClassId,
    string Title,
    int MaxStudents,
    Modality Modality,
    Guid? TeacherUserId) : IRequest<Result<ClassDto>>;

public sealed record CancelClassCommand(Guid ClassId) : IRequest<Result>;

public sealed record GetClassesQuery(int Page = 1, int PageSize = 25, string? Search = null)
    : IRequest<Result<PagedResult<ClassDto>>>;

public sealed record GetClassByIdQuery(Guid ClassId) : IRequest<Result<ClassDto>>;

public sealed record GetAssignedClassesQuery(Guid TeacherUserId) : IRequest<Result<IReadOnlyCollection<ClassDto>>>;

public sealed record EnrollStudentCommand(Guid ClassId, Guid StudentProfileId) : IRequest<Result>;

public sealed record RemoveStudentFromClassCommand(Guid ClassId, Guid StudentProfileId) : IRequest<Result>;

public sealed record GetClassStudentsQuery(Guid ClassId) : IRequest<Result<IReadOnlyCollection<Guid>>>;