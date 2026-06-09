using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Specializations;

public sealed record SpecializationDto(Guid Id, string Code, string Name, bool IsActive);

public sealed record GetSpecializationsQuery(bool IncludeInactive = false)
    : IRequest<Result<IReadOnlyCollection<SpecializationDto>>>;

public sealed record CreateSpecializationCommand(string Code, string Name)
    : IRequest<Result<SpecializationDto>>;

public sealed record UpdateSpecializationCommand(Guid SpecializationId, string Name)
    : IRequest<Result<SpecializationDto>>;

public sealed record SetSpecializationActiveCommand(Guid SpecializationId, bool IsActive)
    : IRequest<Result<SpecializationDto>>;

public sealed record DeleteSpecializationCommand(Guid SpecializationId) : IRequest<Result>;

/// <summary>
/// Replaces the staff member's specialization set with the provided list.
/// Empty list clears all specializations.
/// </summary>
public sealed record SetStaffSpecializationsCommand(Guid StaffProfileId, IReadOnlyList<Guid> SpecializationIds)
    : IRequest<Result<IReadOnlyCollection<SpecializationDto>>>;

public sealed record GetStaffSpecializationsQuery(Guid StaffProfileId)
    : IRequest<Result<IReadOnlyCollection<SpecializationDto>>>;
