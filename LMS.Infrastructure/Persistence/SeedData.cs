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
        var directorRoleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var officeRoleId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var teacherRoleId = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var supportRoleId = Guid.Parse("10000000-0000-0000-0000-000000000004");
        var studentRoleId = Guid.Parse("10000000-0000-0000-0000-000000000005");
        var adminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000006");
        var superAdminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000007");

        b.Entity<Role>().HasData(
            new
            {
                Id = directorRoleId, Code = RoleCodes.AcademyDirector, Name = "Academy Director", CreatedAt = now,
                UpdatedAt = now
            },
            new
            {
                Id = officeRoleId, Code = RoleCodes.OfficeAdmin, Name = "Office Admin", CreatedAt = now, UpdatedAt = now
            },
            new { Id = teacherRoleId, Code = RoleCodes.Teacher, Name = "Teacher", CreatedAt = now, UpdatedAt = now },
            new
            {
                Id = supportRoleId, Code = RoleCodes.SupportTeacher, Name = "Support Teacher", CreatedAt = now,
                UpdatedAt = now
            },
            new { Id = studentRoleId, Code = RoleCodes.Student, Name = "Student", CreatedAt = now, UpdatedAt = now },
            new { Id = adminRoleId, Code = RoleCodes.Admin, Name = "Admin", CreatedAt = now, UpdatedAt = now },
            new
            {
                Id = superAdminRoleId, Code = RoleCodes.SuperAdmin, Name = "Super Admin", CreatedAt = now,
                UpdatedAt = now
            }
        );

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
            UpdatedAt = now
        });

        b.Entity<UserRole>().HasData(new
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000001"), UserId = adminUserId, RoleId = directorRoleId,
            CreatedAt = now, UpdatedAt = now
        }, new
        {
            Id = Guid.Parse("30000000-0000-0000-0000-000000000002"), UserId = adminUserId, RoleId = superAdminRoleId,
            CreatedAt = now, UpdatedAt = now
        });

        b.Entity<Badge>().Property<string>("Code");
        b.Entity<Badge>().HasData(
            new
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000001"), Name = "First Attendance", XpReward = 20,
                Code = "FIRST_ATTENDANCE", CreatedAt = now, UpdatedAt = now
            },
            new
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000002"), Name = "Perfect Week", XpReward = 50,
                Code = "PERFECT_WEEK", CreatedAt = now, UpdatedAt = now
            },
            new
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000003"), Name = "Ten Class Streak", XpReward = 80,
                Code = "TEN_CLASS_STREAK", CreatedAt = now, UpdatedAt = now
            },
            new
            {
                Id = Guid.Parse("40000000-0000-0000-0000-000000000004"), Name = "Homework Hero", XpReward = 40,
                Code = "HOMEWORK_HERO", CreatedAt = now, UpdatedAt = now
            }
        );
    }
}
