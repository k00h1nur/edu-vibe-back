using System.Security.Cryptography;
using System.Text;
using LMS.Application.Common.Security;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using LMS.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using UserRole = LMS.Domain.Entities.UserRole;

namespace LMS.Infrastructure.Persistence;

public static class SeedData
{
    public static void Apply(ModelBuilder b)
    {
        var now = DateTime.UtcNow;

        // ---------- Roles ----------------------------------------------------

        var directorRoleId   = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var officeRoleId     = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var teacherRoleId    = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var supportRoleId    = Guid.Parse("10000000-0000-0000-0000-000000000004");
        var studentRoleId    = Guid.Parse("10000000-0000-0000-0000-000000000005");
        var adminRoleId      = Guid.Parse("10000000-0000-0000-0000-000000000006");
        var superAdminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000007");

        b.Entity<Role>().HasData(
            new { Id = directorRoleId,   Code = RoleCodes.AcademyDirector, Name = "Academy Director", CreatedAt = now, UpdatedAt = now },
            new { Id = officeRoleId,     Code = RoleCodes.OfficeAdmin,     Name = "Office Admin",     CreatedAt = now, UpdatedAt = now },
            new { Id = teacherRoleId,    Code = RoleCodes.Teacher,         Name = "Teacher",          CreatedAt = now, UpdatedAt = now },
            new { Id = supportRoleId,    Code = RoleCodes.SupportTeacher,  Name = "Support Teacher",  CreatedAt = now, UpdatedAt = now },
            new { Id = studentRoleId,    Code = RoleCodes.Student,         Name = "Student",          CreatedAt = now, UpdatedAt = now },
            new { Id = adminRoleId,      Code = RoleCodes.Admin,           Name = "Admin",            CreatedAt = now, UpdatedAt = now },
            new { Id = superAdminRoleId, Code = RoleCodes.SuperAdmin,      Name = "Super Admin",      CreatedAt = now, UpdatedAt = now }
        );

        // ---------- Permissions ---------------------------------------------
        // Deterministic ids so re-running migrations doesn't shuffle rows.
        // Every permission referenced anywhere in the codebase must live in
        // Permissions.All — the discovery service will also auto-add anything
        // referenced by [PermissionAuthorize] that we forgot.

        var permissionIdsByCode = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        foreach (var code in Permissions.All)
        {
            var id = DeterministicGuid("permission:" + code.ToLowerInvariant());
            permissionIdsByCode[code] = id;

            var module = code.Contains('.') ? code.Split('.')[0] : "General";
            b.Entity<Permission>().HasData(new
            {
                Id = id,
                Code = code,
                Module = module,
                Description = (string?)$"Auto-seeded — grants {code}.",
                IsSystem = true,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }

        // ---------- Role → Permission grants --------------------------------
        // Admin gets EVERY permission explicitly. SuperAdmin bypasses checks in
        // PermissionAuthorizationHandler, so it doesn't need explicit grants —
        // but giving it all permissions too keeps the model coherent if someone
        // later removes the bypass.

        SeedRolePermissions(b, adminRoleId,      Permissions.All,                          now);
        SeedRolePermissions(b, superAdminRoleId, Permissions.All,                          now);
        SeedRolePermissions(b, directorRoleId,   RolePermissionMatrix.ForAcademyDirector,  now);
        SeedRolePermissions(b, officeRoleId,     RolePermissionMatrix.ForOfficeAdmin,      now);
        SeedRolePermissions(b, teacherRoleId,    RolePermissionMatrix.ForTeacher,          now);
        SeedRolePermissions(b, supportRoleId,    RolePermissionMatrix.ForSupportTeacher,   now);
        SeedRolePermissions(b, studentRoleId,    RolePermissionMatrix.ForStudent,          now);

        void SeedRolePermissions(ModelBuilder builder, Guid roleId, IEnumerable<string> codes, DateTime ts)
        {
            foreach (var code in codes)
            {
                if (!permissionIdsByCode.TryGetValue(code, out var permissionId)) continue;

                var rpId = DeterministicGuid($"role-permission:{roleId:N}:{code.ToLowerInvariant()}");
                builder.Entity<RolePermission>().HasData(new
                {
                    Id = rpId,
                    RoleId = roleId,
                    PermissionId = permissionId,
                    CreatedAt = ts,
                    UpdatedAt = ts,
                });
            }
        }

        // ---------- Seed admin user -----------------------------------------

        var adminUserId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var adminPasswordHash = PasswordHashing.HashWithSalt("ChangeMe123!", "eduvibe-seed-salt");
        b.Entity<User>().HasData(new
        {
            Id = adminUserId,
            Email = "director@eduvibe.local",
            PasswordHash = adminPasswordHash,
            RefreshTokenHash = (string?)null,
            RefreshTokenExpiresAt = (DateTime?)null,
            Status = UserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
        });

        b.Entity<UserRole>().HasData(
            new
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000001"),
                UserId = adminUserId, RoleId = directorRoleId,
                CreatedAt = now, UpdatedAt = now,
            },
            new
            {
                Id = Guid.Parse("30000000-0000-0000-0000-000000000002"),
                UserId = adminUserId, RoleId = superAdminRoleId,
                CreatedAt = now, UpdatedAt = now,
            });

        // ---------- Badges --------------------------------------------------

        b.Entity<Badge>().Property<string>("Code");
        b.Entity<Badge>().HasData(
            new { Id = Guid.Parse("40000000-0000-0000-0000-000000000001"), Name = "First Attendance",  XpReward = 20, Code = "FIRST_ATTENDANCE", CreatedAt = now, UpdatedAt = now },
            new { Id = Guid.Parse("40000000-0000-0000-0000-000000000002"), Name = "Perfect Week",      XpReward = 50, Code = "PERFECT_WEEK",     CreatedAt = now, UpdatedAt = now },
            new { Id = Guid.Parse("40000000-0000-0000-0000-000000000003"), Name = "Ten Class Streak",  XpReward = 80, Code = "TEN_CLASS_STREAK", CreatedAt = now, UpdatedAt = now },
            new { Id = Guid.Parse("40000000-0000-0000-0000-000000000004"), Name = "Homework Hero",     XpReward = 40, Code = "HOMEWORK_HERO",    CreatedAt = now, UpdatedAt = now }
        );
    }

    /// <summary>
    /// Stable Guid derived from a UTF-8 seed via SHA-1 (RFC 4122 §4.3 namespace-style).
    /// Same input → same Guid across processes and migrations, which keeps EF's seed
    /// diff stable.
    /// </summary>
    private static Guid DeterministicGuid(string seed)
    {
        using var sha = SHA1.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(seed));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);

        // Stamp version (5) and variant (RFC 4122) so the value is a valid v5 Guid.
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x50);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes);
    }
}
