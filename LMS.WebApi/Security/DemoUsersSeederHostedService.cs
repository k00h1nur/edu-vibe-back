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
/// Ensures one demo user per *primary* role exists with a known password so
/// every panel of the LMS admin is loginable out of the box on a fresh
/// database. Strictly idempotent — checks by email and skips if the user is
/// already there, so running it twice is a no-op and running it on a
/// populated database doesn't disturb real accounts.
///
/// We seed only the three roles the UI actually surfaces. The legacy roles
/// (SuperAdmin, AcademyDirector, OfficeAdmin, SupportTeacher) still exist in
/// the <c>roles</c> table so historical user assignments survive — they're
/// simply not seeded with demo users anymore. <see cref="RolePermissionSeederHostedService"/>
/// likewise only tops up the three primary roles.
///
/// What gets created (per role, only if the email is unused):
///   admin@eduvibe.local            → Admin   + StaffProfile (FullTime), first/last name set
///   teacher@eduvibe.local          → Teacher + StaffProfile (FullTime), first/last name set
///   student@eduvibe.local          → Student + StudentProfile, first/last/phone/description set
///   plus 5 sample students (sarah.chen@, …) so the Manage Roster modal
///   has a non-empty list out of the box.
///
/// Default password for all of them: <c>Demo!2026</c>.
/// Override the password (and toggle the seeder off entirely) via config:
///   "DemoUsers:Enabled": true|false   (default FALSE — on only in Development)
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
    /// <summary>
    /// Tuple of (localPart, RoleCode, ProfileKind, FirstName, LastName, PhoneNumber,
    /// Description). Profile fields are optional — null skips them.
    /// </summary>
    private static readonly DemoUserSpec[] Spec =
    {
        // Primary role accounts — one per panel, with sensible profile data so
        // every screen renders with real names instead of email-derived
        // fallbacks.
        new("admin",   RoleCodes.Admin,   ProfileKind.Staff,   "Nigora",  "Karimova", "+998 90 100 00 01", "Academy director"),
        new("teacher", RoleCodes.Teacher, ProfileKind.Staff,   "Dilshod", "Tursunov", "+998 90 100 00 02", "Lead instructor"),
        new("student", RoleCodes.Student, ProfileKind.Student, "Aziza",   "Karimova", "+998 90 100 00 03", "Sample CEFR B1 learner"),

        // Five extra students so the roster picker in /admin/classes/* isn't
        // empty. Phone numbers are obviously fake (UZ +998 placeholder).
        new("sarah.chen",   RoleCodes.Student, ProfileKind.Student, "Sarah",   "Chen",      "+998 90 200 00 01", null),
        new("mike.brown",   RoleCodes.Student, ProfileKind.Student, "Michael", "Brown",     "+998 90 200 00 02", null),
        new("anna.ivanova", RoleCodes.Student, ProfileKind.Student, "Anna",    "Ivanova",   "+998 90 200 00 03", null),
        new("jamol.alimov", RoleCodes.Student, ProfileKind.Student, "Jamol",   "Alimov",    "+998 90 200 00 04", null),
        new("nadia.usmon",  RoleCodes.Student, ProfileKind.Student, "Nadia",   "Usmonova",  "+998 90 200 00 05", null),
    };

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Default OFF (defense-in-depth): a forgotten prod override must NOT create
        // a known-credential admin account. appsettings.Development.json turns it on.
        if (!configuration.GetValue("DemoUsers:Enabled", false))
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

        var created = 0;
        var profilesPatched = 0;
        var passwordHash = hasher.Hash(password);

        foreach (var s in Spec)
        {
            var email = $"{s.LocalPart}@{domain}".ToLowerInvariant();

            if (!roleIdByCode.TryGetValue(s.RoleCode, out var roleId))
            {
                logger.LogWarning("Role {Role} missing — skipping demo user {Email}.", s.RoleCode, email);
                continue;
            }

            var existing = await db.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

            if (existing is not null)
            {
                // Belt-and-braces: a previously-seeded user might be missing
                // its role assignment if someone wiped the user_roles table.
                // Add it back without touching the existing password or profile.
                if (!existing.UserRoles.Any(ur => ur.RoleId == roleId))
                {
                    await db.UserRoles.AddAsync(new UserRoleEntity(existing.Id, roleId), cancellationToken);
                    await db.SaveChangesAsync(cancellationToken);
                    logger.LogInformation(
                        "Demo user {Email} existed but was missing role {Role} — re-attached.",
                        email, s.RoleCode);
                }
                // Patch in profile fields that were added in later migrations
                // (FirstName / LastName / PhoneNumber). Existing seeded users
                // pre-dating those migrations would otherwise stay blank.
                profilesPatched += await PatchProfileFieldsAsync(db, existing.Id, s, cancellationToken);
                continue;
            }

            var user = new User(email, passwordHash);
            await db.Users.AddAsync(user, cancellationToken);
            await db.SaveChangesAsync(cancellationToken); // persist to obtain Id

            await db.UserRoles.AddAsync(new UserRoleEntity(user.Id, roleId), cancellationToken);

            switch (s.Profile)
            {
                case ProfileKind.Staff:
                    var staff = new StaffProfile(user.Id, EmploymentType.FullTime);
                    if (HasAnyProfileField(s))
                    {
                        staff.UpdateProfile(s.FirstName, s.LastName, s.PhoneNumber, s.Description);
                    }
                    await db.StaffProfiles.AddAsync(staff, cancellationToken);
                    break;
                case ProfileKind.Student:
                    var student = new StudentProfile(user.Id, user);
                    if (HasAnyProfileField(s))
                    {
                        student.UpdateProfile(s.FirstName, s.LastName, s.PhoneNumber, s.Description);
                    }
                    await db.StudentProfiles.AddAsync(student, cancellationToken);
                    break;
                case ProfileKind.None:
                    break;
            }

            await db.SaveChangesAsync(cancellationToken);
            created++;
            logger.LogInformation("Demo user created: {Email} ({Role}).", email, s.RoleCode);
        }

        if (created > 0)
        {
            logger.LogInformation(
                "Demo users ready. Default password: {Password} — change before exposing to anyone.",
                password);
        }
        if (profilesPatched > 0)
        {
            logger.LogInformation(
                "Patched profile fields (name/phone/description) onto {Count} pre-existing demo user(s).",
                profilesPatched);
        }
    }

    /// <summary>
    /// Fills in StudentProfile / StaffProfile fields when an old seeded user
    /// is missing them. Idempotent — only writes when at least one target
    /// field is currently null AND the spec has a value for it.
    /// </summary>
    private static async Task<int> PatchProfileFieldsAsync(
        IApplicationDbContext db, Guid userId, DemoUserSpec s, CancellationToken ct)
    {
        if (!HasAnyProfileField(s)) return 0;

        switch (s.Profile)
        {
            case ProfileKind.Staff:
            {
                var profile = await db.StaffProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
                if (profile is null)
                {
                    // Old seeded staff users (e.g. the admin) created before the
                    // seeder attached a profile have none — provision it now so
                    // /api/Staff/me works and they can edit their own settings.
                    profile = new StaffProfile(userId, EmploymentType.FullTime);
                    profile.UpdateProfile(s.FirstName, s.LastName, s.PhoneNumber, s.Description);
                    await db.StaffProfiles.AddAsync(profile, ct);
                    await db.SaveChangesAsync(ct);
                    return 1;
                }
                if (profile.FirstName != null && profile.LastName != null && profile.PhoneNumber != null) return 0;
                profile.UpdateProfile(s.FirstName, s.LastName, s.PhoneNumber, s.Description);
                await db.SaveChangesAsync(ct);
                return 1;
            }
            case ProfileKind.Student:
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
                var profile = await db.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
                if (profile is null)
                {
                    if (user is null) return 0;
                    profile = new StudentProfile(userId, user);
                    profile.UpdateProfile(s.FirstName, s.LastName, s.PhoneNumber, s.Description);
                    await db.StudentProfiles.AddAsync(profile, ct);
                    await db.SaveChangesAsync(ct);
                    return 1;
                }
                if (profile.FirstName != null && profile.LastName != null && profile.PhoneNumber != null) return 0;
                profile.UpdateProfile(s.FirstName, s.LastName, s.PhoneNumber, s.Description);
                await db.SaveChangesAsync(ct);
                return 1;
            }
            default:
                return 0;
        }
    }

    private static bool HasAnyProfileField(DemoUserSpec s) =>
        !string.IsNullOrWhiteSpace(s.FirstName) ||
        !string.IsNullOrWhiteSpace(s.LastName) ||
        !string.IsNullOrWhiteSpace(s.PhoneNumber) ||
        !string.IsNullOrWhiteSpace(s.Description);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private enum ProfileKind { None, Staff, Student }

    private sealed record DemoUserSpec(
        string LocalPart,
        string RoleCode,
        ProfileKind Profile,
        string? FirstName,
        string? LastName,
        string? PhoneNumber,
        string? Description);
}
