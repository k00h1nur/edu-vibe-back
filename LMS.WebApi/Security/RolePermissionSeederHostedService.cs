using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Security;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebApi.Security;

/// <summary>
/// Applies <see cref="RolePermissionMatrix"/> defaults the first time a role
/// has no grants at all. Once a role has any rows in <c>role_permissions</c>
/// — whether seeded here previously or created via the admin API — this
/// service leaves it alone forever.
///
/// Why bootstrap-once instead of "INSERT any missing":
///   • If an admin revokes a default grant via the API, a strictly-additive
///     seeder would silently re-add it on the next boot, undoing the change.
///   • Per-role granularity is the right level: a brand-new role (added via
///     the admin UI later) starts empty and would get NO defaults, but new
///     roles aren't covered by the matrix anyway — they're admin-curated.
///
/// Runs immediately after <see cref="PermissionDiscoveryHostedService"/> in
/// <c>Program.cs</c>'s registration order, so the <c>permissions</c> table
/// is guaranteed populated when we look up ids by code.
/// </summary>
public sealed class RolePermissionSeederHostedService(
    IServiceProvider serviceProvider,
    ILogger<RolePermissionSeederHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var permissionIdByCode = await db.Permissions
            .ToDictionaryAsync(p => p.Code, p => p.Id,
                StringComparer.OrdinalIgnoreCase, cancellationToken);

        if (permissionIdByCode.Count == 0)
        {
            logger.LogWarning(
                "Permissions table is empty — skipping default grant bootstrap. " +
                "PermissionDiscoveryHostedService should run first.");
            return;
        }

        var roleIdByCode = await db.Roles
            .ToDictionaryAsync(r => r.Code, r => r.Id,
                StringComparer.OrdinalIgnoreCase, cancellationToken);

        // Roles that already have ANY grants are considered "initialized" and
        // off-limits — admin customizations (additions or removals) are
        // preserved as-is.
        var initializedRoleIds = await db.RolePermissions
            .Select(rp => rp.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);
        var initialized = initializedRoleIds.ToHashSet();

        // (RoleCode → permission codes to grant on first bootstrap).
        var defaults = new (string RoleCode, IEnumerable<string> Codes)[]
        {
            (RoleCodes.SuperAdmin,      Permissions.All),
            (RoleCodes.Admin,           Permissions.All),
            (RoleCodes.AcademyDirector, RolePermissionMatrix.ForAcademyDirector),
            (RoleCodes.OfficeAdmin,     RolePermissionMatrix.ForOfficeAdmin),
            (RoleCodes.Teacher,         RolePermissionMatrix.ForTeacher),
            (RoleCodes.SupportTeacher,  RolePermissionMatrix.ForSupportTeacher),
            (RoleCodes.Student,         RolePermissionMatrix.ForStudent),
        };

        var bootstrappedRoles = 0;
        var insertedRows = 0;

        foreach (var (roleCode, codes) in defaults)
        {
            if (!roleIdByCode.TryGetValue(roleCode, out var roleId))
            {
                logger.LogWarning("Role {Role} not found in DB — skipping bootstrap.", roleCode);
                continue;
            }

            if (initialized.Contains(roleId))
            {
                logger.LogDebug(
                    "Role {Role} already has grants — leaving admin configuration untouched.",
                    roleCode);
                continue;
            }

            foreach (var code in codes)
            {
                if (!permissionIdByCode.TryGetValue(code, out var permissionId))
                {
                    logger.LogWarning(
                        "Default permission {Permission} for role {Role} is not in the DB — skipping.",
                        code, roleCode);
                    continue;
                }

                await db.RolePermissions.AddAsync(new RolePermission(roleId, permissionId),
                    cancellationToken);
                insertedRows++;
            }
            bootstrappedRoles++;
        }

        if (insertedRows > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation(
                "Bootstrapped default permissions for {Roles} role(s) ({Rows} grants).",
                bootstrappedRoles, insertedRows);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
