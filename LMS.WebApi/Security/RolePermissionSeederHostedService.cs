using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Security;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebApi.Security;

/// <summary>
/// Applies the <see cref="RolePermissionMatrix"/> defaults to the database on
/// every startup. Strictly additive — it only INSERTs missing grants and
/// never deletes or rewrites existing ones, so admins can edit grants via
/// the API without their changes being clobbered on next boot.
///
/// Runs after <see cref="PermissionDiscoveryHostedService"/> in
/// <c>Program.cs</c>'s registration order — permission rows are guaranteed
/// to exist by the time we look them up.
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
                "Permissions table is empty — skipping role grant seeding. " +
                "PermissionDiscoveryHostedService should have populated it first.");
            return;
        }

        var roleIdByCode = await db.Roles
            .ToDictionaryAsync(r => r.Code, r => r.Id,
                StringComparer.OrdinalIgnoreCase, cancellationToken);

        // (RoleCode → permission codes to ensure are granted). Admin /
        // SuperAdmin get the whole catalog; the rest get scoped subsets.
        var plan = new (string RoleCode, IEnumerable<string> Codes)[]
        {
            (RoleCodes.SuperAdmin,      Permissions.All),
            (RoleCodes.Admin,           Permissions.All),
            (RoleCodes.AcademyDirector, RolePermissionMatrix.ForAcademyDirector),
            (RoleCodes.OfficeAdmin,     RolePermissionMatrix.ForOfficeAdmin),
            (RoleCodes.Teacher,         RolePermissionMatrix.ForTeacher),
            (RoleCodes.SupportTeacher,  RolePermissionMatrix.ForSupportTeacher),
            (RoleCodes.Student,         RolePermissionMatrix.ForStudent),
        };

        var added = 0;
        foreach (var (roleCode, codes) in plan)
        {
            if (!roleIdByCode.TryGetValue(roleCode, out var roleId))
            {
                logger.LogWarning("Role {Role} not found — skipping grants for it.", roleCode);
                continue;
            }

            var existing = await db.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.PermissionId)
                .ToListAsync(cancellationToken);
            var existingSet = existing.ToHashSet();

            foreach (var code in codes)
            {
                if (!permissionIdByCode.TryGetValue(code, out var permissionId))
                {
                    logger.LogWarning(
                        "Permission {Permission} referenced by role {Role} doesn't exist in the DB — skipping.",
                        code, roleCode);
                    continue;
                }
                if (existingSet.Contains(permissionId)) continue;

                await db.RolePermissions.AddAsync(new RolePermission(roleId, permissionId),
                    cancellationToken);
                added++;
            }
        }

        if (added > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Granted {Count} role-permission rows from RolePermissionMatrix.", added);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
