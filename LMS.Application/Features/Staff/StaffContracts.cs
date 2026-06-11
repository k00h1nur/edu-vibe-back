using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Staff;

public sealed record StaffDto(
    Guid Id,
    Guid UserId,
    string Email,
    EmploymentType EmploymentType,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Description,
    string? AvatarUrl,
    UserStatus Status,
    string? Position,
    bool IsPubliclyVisible);

public sealed record SetStaffStatusCommand(Guid StaffProfileId, UserStatus Status)
    : IRequest<Result<StaffDto>>;

/// <summary>
/// Toggle whether this staff member appears on the marketing-site teachers
/// grid. Defaults to off — admin opts staff in explicitly.
/// </summary>
public sealed record SetStaffPublicVisibilityCommand(Guid StaffProfileId, bool IsPubliclyVisible)
    : IRequest<Result<StaffDto>>;

public sealed record SetStaffAvatarCommand(Guid StaffProfileId, string? AvatarUrl)
    : IRequest<Result<StaffDto>>;

public sealed record StaffPingCommand : IRequest<Result<string>>;

public sealed class StaffPingCommandHandler : IRequestHandler<StaffPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(StaffPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Staff module ready"));
    }
}

public sealed record CreateStaffCommand(Guid UserId, EmploymentType EmploymentType) : IRequest<Result<StaffDto>>;

public sealed record UpdateStaffProfileCommand(Guid StaffProfileId, EmploymentType EmploymentType)
    : IRequest<Result<StaffDto>>;

/// <summary>
/// Updates the editable profile fields (name, phone, description, position).
/// Employment type is updated separately via <see cref="UpdateStaffProfileCommand"/>.
/// </summary>
public sealed record UpdateStaffDetailsCommand(
    Guid StaffProfileId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Description,
    string? Position) : IRequest<Result<StaffDto>>;

public sealed record GetStaffQuery(int Page = 1, int PageSize = 25, string? Search = null)
    : IRequest<Result<PagedResult<StaffDto>>>;

/// <summary>Returns the staff profile linked to the currently authenticated user.</summary>
public sealed record GetMyStaffProfileQuery : IRequest<Result<StaffDto>>;

/// <summary>Lean public shape — what the marketing site renders on the teachers grid.</summary>
public sealed record PublicTeacherDto(
    Guid Id,
    string FullName,
    string? Position,
    string? Description,
    string? AvatarUrl,
    IReadOnlyCollection<string> Specializations);

public sealed record GetPublicTeachersQuery(int Take = 30)
    : IRequest<Result<IReadOnlyCollection<PublicTeacherDto>>>;
