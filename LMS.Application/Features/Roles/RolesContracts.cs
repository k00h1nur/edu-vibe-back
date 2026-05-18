using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Roles;

public sealed record RoleDto(Guid Id, string Code, string Name, IReadOnlyCollection<Guid> PermissionIds);
public sealed record PermissionDto(Guid Id, string Code, string Module, string? Description);

public sealed record GetRolesQuery() : IRequest<Result<IReadOnlyCollection<RoleDto>>>;
public sealed record GetPermissionsQuery() : IRequest<Result<IReadOnlyCollection<PermissionDto>>>;
public sealed record CreateRoleCommand(string Code, string Name) : IRequest<Result<RoleDto>>;
public sealed record UpdateRoleCommand(Guid RoleId, string Code, string Name) : IRequest<Result<RoleDto>>;
public sealed record DeleteRoleCommand(Guid RoleId) : IRequest<Result>;
public sealed record CreatePermissionCommand(string Code, string Module, string? Description) : IRequest<Result<PermissionDto>>;
public sealed record UpdatePermissionCommand(Guid PermissionId, string Code, string Module, string? Description) : IRequest<Result<PermissionDto>>;
public sealed record DeletePermissionCommand(Guid PermissionId) : IRequest<Result>;
public sealed record AssignRolePermissionsCommand(Guid RoleId, IReadOnlyCollection<Guid> PermissionIds) : IRequest<Result>;
