using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Roles;

public sealed class RolesHandlers(IApplicationDbContext db) :
    IRequestHandler<GetRolesQuery, Result<IReadOnlyCollection<RoleDto>>>,
    IRequestHandler<GetPermissionsQuery, Result<IReadOnlyCollection<PermissionDto>>>,
    IRequestHandler<GetAccessOverviewQuery, Result<IReadOnlyCollection<UserAccessDto>>>,
    IRequestHandler<CreateRoleCommand, Result<RoleDto>>,
    IRequestHandler<UpdateRoleCommand, Result<RoleDto>>,
    IRequestHandler<DeleteRoleCommand, Result>,
    IRequestHandler<CreatePermissionCommand, Result<PermissionDto>>,
    IRequestHandler<UpdatePermissionCommand, Result<PermissionDto>>,
    IRequestHandler<DeletePermissionCommand, Result>,
    IRequestHandler<AssignRolePermissionsCommand, Result>
{
    // Permission codes that ship in the code catalog (Permissions.All) are
    // "system" — deleting one would just be re-created on next boot by the
    // discovery seeder, and could break authz until then. Only CUSTOM codes
    // (created via this API, absent from the catalog) may be deleted.
    private static readonly IReadOnlySet<string> SystemPermissionCodes =
        new HashSet<string>(Permissions.All, StringComparer.OrdinalIgnoreCase);

    public async Task<Result<IReadOnlyCollection<RoleDto>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await db.Roles.AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new RoleDto(r.Id, r.Code, r.Name,
                db.RolePermissions.Where(x => x.RoleId == r.Id).Select(x => x.PermissionId).ToList())
            {
                IsBuiltIn = RoleCodes.BuiltIn.Contains(r.Code),
            }).ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<RoleDto>>.Ok(roles);
    }

    public async Task<Result<IReadOnlyCollection<PermissionDto>>> Handle(GetPermissionsQuery request, CancellationToken cancellationToken)
    {
        var rows = await db.Permissions.AsNoTracking().OrderBy(x => x.Module).ThenBy(x => x.Code)
            .Select(p => new { p.Id, p.Code, p.Module, p.Description }).ToListAsync(cancellationToken);
        var list = rows.Select(p => new PermissionDto(p.Id, p.Code, p.Module, p.Description)
        {
            IsSystem = SystemPermissionCodes.Contains(p.Code),
        }).ToList();
        return Result<IReadOnlyCollection<PermissionDto>>.Ok(list);
    }

    public async Task<Result<IReadOnlyCollection<UserAccessDto>>> Handle(GetAccessOverviewQuery request, CancellationToken cancellationToken)
    {
        // Per-user roles (codes) + the distinct permission codes those roles
        // grant, so a SuperAdmin can audit "who can access what" in one screen.
        var users = await db.Users.AsNoTracking()
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.Status,
                Roles = db.UserRoles.Where(ur => ur.UserId == u.Id)
                    .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => r.Code).ToList(),
                Permissions = db.UserRoles.Where(ur => ur.UserId == u.Id)
                    .Join(db.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp.PermissionId)
                    .Join(db.Permissions, pid => pid, p => p.Id, (pid, p) => p.Code)
                    .Distinct().ToList(),
            })
            .ToListAsync(cancellationToken);

        var overview = users.Select(u =>
        {
            var isSuper = u.Roles.Contains(RoleCodes.SuperAdmin, StringComparer.OrdinalIgnoreCase);
            return new UserAccessDto(u.Id, u.Email, u.Status,
                u.Roles.OrderBy(x => x).ToList(),
                u.Permissions.OrderBy(x => x).ToList(),
                isSuper);
        }).ToList();

        return Result<IReadOnlyCollection<UserAccessDto>>.Ok(overview);
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

        // A built-in role's Code is structural (auth stack + seeders reference it),
        // so it can be renamed (Name) but its Code must stay put.
        var isBuiltIn = RoleCodes.IsBuiltIn(r.Code);
        if (isBuiltIn && !string.Equals(r.Code, request.Code, StringComparison.Ordinal))
            return Result<RoleDto>.Fail("BUILTIN_ROLE",
                "A built-in role's code can't be changed. You can rename it, or edit its permissions.");

        r.Update(request.Code, request.Name);
        await db.SaveChangesAsync(cancellationToken);
        var pids = await db.RolePermissions.Where(x => x.RoleId == r.Id).Select(x => x.PermissionId).ToListAsync(cancellationToken);
        return Result<RoleDto>.Ok(new RoleDto(r.Id, r.Code, r.Name, pids) { IsBuiltIn = isBuiltIn });
    }

    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var r = await db.Roles.FirstOrDefaultAsync(x => x.Id == request.RoleId, cancellationToken);
        if (r is null) return Result.Fail("NOT_FOUND", "Role not found.");
        if (RoleCodes.IsBuiltIn(r.Code))
            return Result.Fail("BUILTIN_ROLE", "Built-in roles can't be deleted.");

        // Don't strand user assignments as orphan rows — clear them with the role.
        var grants = await db.RolePermissions.Where(x => x.RoleId == r.Id).ToListAsync(cancellationToken);
        db.RolePermissions.RemoveRange(grants);
        var assignments = await db.UserRoles.Where(x => x.RoleId == r.Id).ToListAsync(cancellationToken);
        db.UserRoles.RemoveRange(assignments);
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
        p.Update(request.Code, request.Module, request.Description);
        await db.SaveChangesAsync(cancellationToken);
        return Result<PermissionDto>.Ok(new PermissionDto(p.Id, p.Code, p.Module, p.Description));
    }

    public async Task<Result> Handle(DeletePermissionCommand request, CancellationToken cancellationToken)
    {
        var p = await db.Permissions.FirstOrDefaultAsync(x => x.Id == request.PermissionId, cancellationToken);
        if (p is null) return Result.Fail("NOT_FOUND", "Permission not found.");
        if (SystemPermissionCodes.Contains(p.Code))
            return Result.Fail("SYSTEM_PERMISSION",
                "System permissions can't be deleted (they'd be re-created on the next boot).");

        // Remove any role grants of this permission alongside it.
        var grants = await db.RolePermissions.Where(x => x.PermissionId == p.Id).ToListAsync(cancellationToken);
        db.RolePermissions.RemoveRange(grants);
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
