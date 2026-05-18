using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Roles;

public sealed class RolesHandlers(IApplicationDbContext db) :
    IRequestHandler<GetRolesQuery, Result<IReadOnlyCollection<RoleDto>>>,
    IRequestHandler<GetPermissionsQuery, Result<IReadOnlyCollection<PermissionDto>>>,
    IRequestHandler<CreateRoleCommand, Result<RoleDto>>,
    IRequestHandler<UpdateRoleCommand, Result<RoleDto>>,
    IRequestHandler<DeleteRoleCommand, Result>,
    IRequestHandler<CreatePermissionCommand, Result<PermissionDto>>,
    IRequestHandler<UpdatePermissionCommand, Result<PermissionDto>>,
    IRequestHandler<DeletePermissionCommand, Result>,
    IRequestHandler<AssignRolePermissionsCommand, Result>
{
    public async Task<Result<IReadOnlyCollection<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await db.Roles.Select(r => new RoleDto(r.Id, r.Code, r.Name,
            db.RolePermissions.Where(x => x.RoleId == r.Id).Select(x => x.PermissionId).ToList())).ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<RoleDto>>.Ok(roles);
    }

    public async Task<Result<IReadOnlyCollection<PermissionDto>>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var list = await db.Permissions.OrderBy(x => x.Module).ThenBy(x => x.Code)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Module, p.Description)).ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<PermissionDto>>.Ok(list);
    }

    public async Task<Result<RoleDto>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (await db.Roles.AnyAsync(x => x.Code == request.Code, cancellationToken))
            return Result<RoleDto>.Fail("DUPLICATE", "Role code already exists.");
        var r = new Role(request.Code, request.Name);
        await db.Roles.AddAsync(r, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<RoleDto>.Ok(new RoleDto(r.Id, r.Code, r.Name, Array.Empty<Guid>()));
    }

    public async Task<Result<RoleDto>> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var r = await db.Roles.FirstOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);
        if (r is null) return Result<RoleDto>.Fail("NOT_FOUND", "Role not found.");
        typeof(Role).GetProperty(nameof(Role.Code))!.SetValue(r, request.Code.Trim());
        typeof(Role).GetProperty(nameof(Role.Name))!.SetValue(r, request.Name.Trim());
        await db.SaveChangesAsync(cancellationToken);
        var pids = await db.RolePermissions.Where(x => x.RoleId == r.Id).Select(x => x.PermissionId).ToListAsync(cancellationToken);
        return Result<RoleDto>.Ok(new RoleDto(r.Id, r.Code, r.Name, pids));
    }

    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var r = await db.Roles.FirstOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);
        if (r is null) return Result.Fail("NOT_FOUND", "Role not found.");
        db.Roles.Remove(r);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Role deleted.");
    }

    public async Task<Result<PermissionDto>> Handle(CreatePermissionCommand request, CancellationToken cancellationToken)
    {
        if (await db.Permissions.AnyAsync(x => x.Code == request.Code, cancellationToken))
            return Result<PermissionDto>.Fail("DUPLICATE", "Permission code already exists.");
        var p = new Permission(request.Code, request.Module, request.Description);
        await db.Permissions.AddAsync(p, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<PermissionDto>.Ok(new PermissionDto(p.Id, p.Code, p.Module, p.Description));
    }

    public async Task<Result<PermissionDto>> Handle(UpdatePermissionCommand request, CancellationToken cancellationToken)
    {
        var p = await db.Permissions.FirstOrDefaultAsync(x => x.Id == request.PermissionId, cancellationToken);
        if (p is null) return Result<PermissionDto>.Fail("NOT_FOUND", "Permission not found.");
        typeof(Permission).GetProperty(nameof(Permission.Code))!.SetValue(p, request.Code.Trim());
        typeof(Permission).GetProperty(nameof(Permission.Module))!.SetValue(p, request.Module.Trim());
        typeof(Permission).GetProperty(nameof(Permission.Description))!.SetValue(p, request.Description?.Trim());
        await db.SaveChangesAsync(cancellationToken);
        return Result<PermissionDto>.Ok(new PermissionDto(p.Id, p.Code, p.Module, p.Description));
    }

    public async Task<Result> Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
    {
        var p = await db.Permissions.FirstOrDefaultAsync(x => x.Id == request.PermissionId, cancellationToken);
        if (p is null) return Result.Fail("NOT_FOUND", "Permission not found.");
        db.Permissions.Remove(p);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Permission deleted.");
    }

    public async Task<Result> Handle(AssignRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);
        if (role is null) return Result.Fail("NOT_FOUND", "Role not found.");

        var current = await db.RolePermissions.Where(x => x.RoleId == role.Id).ToListAsync(cancellationToken);
        db.RolePermissions.RemoveRange(current);

        foreach (var pid in request.PermissionIds.Distinct())
        {
            await db.RolePermissions.AddAsync(new RolePermission(role.Id, pid), cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Role permissions updated.");
    }
}
