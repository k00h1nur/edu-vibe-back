using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Security;
using LMS.Domain.Entities;
// Alias the enum to dodge the name clash with LMS.Domain.Entities.UserRole
// (the join entity vs. the enum). Entities are referenced by their short name
// throughout the seeder; the enum is rarely used.
using LMS.Domain.Enums;
using UserRoleEntity = LMS.Domain.Entities.UserRole;
using Microsoft.EntityFrameworkCore;

namespace LMS.WebApi.Security;

/// <summary>
/// Ensures one demo user per role exists with a known password so every panel
/// of the LMS admin is loginable out of the box on a fresh database. Strictly
/// idempotent — checks by email and skips if the user is already there, so
/// running it twice is a no-op and running it on a populated database doesn't
/// disturb real accounts.
///
/// What gets created (per role, only if the email is unused):
///   superadmin@eduvibe.local       → SuperAdmin
///   admin@eduvibe.local            → Admin
///   director@eduvibe.local         → AcademyDirector
///   office@eduvibe.local           → OfficeAdmin
///   teacher@eduvibe.local          → Teacher       + StaffProfile (FullTime)
///   support@eduvibe.local          → SupportTeacher + StaffProfile (FullTime)
///   student@eduvibe.local          → Student        + StudentProfile
///
/// Default password for all of them: <c>Demo!2026</c>.
/// Override the password (and toggle the seeder off entirely) via config:
///   "DemoUsers:Enabled": true|false   (default true)
///   "DemoUsers:Password": "..."       (default "Demo!2026")
///   "DemoUsers:EmailDomain": "..."     (default "eduvibe.local")
///
/// Runs AFTER the role/permission seeders so role lookups succeed.
/// </summary>
public sealed class DemoUsersSeederHostedService(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    ILogger<DemoUsersSeederHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!configuration.GetValue("DemoUsers:Enabled", true))
        {
            logger.LogInformation("Demo users seeder disabled by config.");
            return;
        }

        var password = configuration["DemoUsers:Password"] ?? "Demo!2026";
        var domain = configuration["DemoUsers:EmailDomain"] ?? "eduvibe.local";

        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        var roleIdByCode = await db.Roles
            .ToDictionaryAsync(r => r.Code, r => r.Id,
                StringComparer.OrdinalIgnoreCase, cancellationToken);

        if (roleIdByCode.Count == 0)
        {
            logger.LogWarning("Roles table is empty — skipping demo users.");
            return;
        }

        var spec = new (string LocalPart, string RoleCode, ProfileKind Profile)[]
        {
            ("superadmin", RoleCodes.SuperAdmin,      ProfileKind.None),
            ("admin",      RoleCodes.Admin,           ProfileKind.None),
            ("director",   RoleCodes.AcademyDirector, ProfileKind.Staff),
            ("office",     RoleCodes.OfficeAdmin,     ProfileKind.Staff),
            ("teacher",    RoleCodes.Teacher,         ProfileKind.Staff),
            ("support",    RoleCodes.SupportTeacher,  ProfileKind.Staff),
            ("student",    RoleCodes.Student,         ProfileKind.Student),
        };

        var created = 0;
        var passwordHash = hasher.Hash(password);

        foreach (var (localPart, roleCode, profile) in spec)
        {
            var email = $"{localPart}@{domain}".ToLowerInvariant();

            if (!roleIdByCode.TryGetValue(roleCode, out var roleId))
            {
                logger.LogWarning("Role {Role} missing — skipping demo user {Email}.", roleCode, email);
                continue;
            }

            var existing = await db.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (existing is not null)
            {
                // Belt-and-braces: a previously-seeded user might be missing its
                // role assignment if someone wiped the user_roles table. Add it
                // back without touching the existing password or profile.
                if (!existing.UserRoles.Any(ur => ur.RoleId == roleId))
                {
                    await db.UserRoles.AddAsync(new UserRoleEntity(existing.Id, roleId), cancellationToken);
                    await db.SaveChangesAsync(cancellationToken);
                    logger.LogInformation(
                        "Demo user {Email} existed but was missing role {Role} — re-attached.",
                        email, roleCode);
                }
                continue;
            }

            var user = new User(email, passwordHash);
            await db.Users.AddAsync(user, cancellationToken);
            await db.SaveChangesAsync(cancellationToken); // persist to obtain Id

            await db.UserRoles.AddAsync(new UserRoleEntity(user.Id, roleId), cancellationToken);

            switch (profile)
            {
                case ProfileKind.Staff:
                    await db.StaffProfiles.AddAsync(
                        new StaffProfile(user.Id, EmploymentType.FullTime), cancellationToken);
                    break;
                case ProfileKind.Student:
                    await db.StudentProfiles.AddAsync(
                        new StudentProfile(user.Id, user), cancellationToken);
                    break;
                case ProfileKind.None:
                    break;
            }

            await db.SaveChangesAsync(cancellationToken);
            created++;
            logger.LogInformation("Demo user created: {Email} ({Role}).", email, roleCode);
        }

        if (created > 0)
        {
            logger.LogInformation(
                "Demo users ready. Default password: {Password} — change before exposing to anyone.",
                password);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private enum ProfileKind { None, Staff, Student }
}
