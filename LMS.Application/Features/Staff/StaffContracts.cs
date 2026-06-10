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
    UserStatus Status);

public sealed record SetStaffStatusCommand(Guid StaffProfileId, UserStatus Status)
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
/// Updates the editable profile fields (name, phone, description). Employment
/// type is updated separately via <see cref="UpdateStaffProfileCommand"/>.
/// </summary>
public sealed record UpdateStaffDetailsCommand(
    Guid StaffProfileId,
    string? FirstName,
    string? LastName,
    string? PhoneNumber,
    string? Description) : IRequest<Result<StaffDto>>;

public sealed record GetStaffQuery(int Page = 1, int PageSize = 25, string? Search = null)
    : IRequest<Result<PagedResult<StaffDto>>>;

/// <summary>Returns the staff profile linked to the currently authenticated user.</summary>
public sealed record GetMyStaffProfileQuery : IRequest<Result<StaffDto>>;
