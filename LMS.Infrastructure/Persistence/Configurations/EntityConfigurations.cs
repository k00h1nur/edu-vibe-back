using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("users");
        b.HasKey(x => x.Id);
        b.Property(x => x.Email).IsRequired().HasMaxLength(320);
        b.Property(x => x.PasswordHash).IsRequired().HasMaxLength(512);
        b.Property(x => x.RefreshTokenHash).HasMaxLength(512);
        b.HasIndex(x => x.Email).IsUnique();
    }
}

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("roles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).IsRequired().HasMaxLength(64);
        b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> b)
    {
        b.ToTable("permissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).IsRequired().HasMaxLength(128);
        b.Property(x => x.Module).IsRequired().HasMaxLength(64);
        b.Property(x => x.Description).HasMaxLength(512);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> b)
    {
        b.ToTable("role_permissions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.RoleId, x.PermissionId }).IsUnique();
        b.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> b)
    {
        b.ToTable("user_roles");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        b.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StaffProfileConfiguration : IEntityTypeConfiguration<StaffProfile>
{
    public void Configure(EntityTypeBuilder<StaffProfile> b)
    {
        b.ToTable("staff_profiles");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasOne(x => x.User).WithOne(x => x.StaffProfile).HasForeignKey<StaffProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> b)
    {
        b.ToTable("student_profiles");
        b.HasKey(x => x.Id);
        b.HasIndex(x => x.UserId).IsUnique();
        b.HasOne(x => x.User).WithOne(x => x.StudentProfile).HasForeignKey<StudentProfile>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> b)
    {
        b.ToTable("courses");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).IsRequired().HasMaxLength(64);
        b.Property(x => x.Name).IsRequired().HasMaxLength(256);
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> b)
    {
        b.ToTable("rooms");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        b.Property(x => x.MeetingLink).HasMaxLength(1024);
        b.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class ClassConfiguration : IEntityTypeConfiguration<Class>
{
    public void Configure(EntityTypeBuilder<Class> b)
    {
        b.ToTable("classes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).IsRequired().HasMaxLength(256);
        b.HasOne(x => x.Teacher).WithMany(x => x.TeachingClasses).HasForeignKey(x => x.TeacherUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> b)
    {
        b.ToTable("enrollments");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ClassId, x.StudentProfileId }).IsUnique();
    }
}

public sealed class ClassSessionConfiguration : IEntityTypeConfiguration<ClassSession>
{
    public void Configure(EntityTypeBuilder<ClassSession> b)
    {
        b.ToTable("class_sessions");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ClassId, x.SessionDate, x.StartsAt }).IsUnique();
    }
}

public sealed class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> b)
    {
        b.ToTable("attendance");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.SessionId, x.StudentProfileId }).IsUnique();
    }
}

public sealed class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> b)
    {
        b.ToTable("assignments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).IsRequired().HasMaxLength(256);
    }
}

public sealed class SubmissionConfiguration : IEntityTypeConfiguration<Submission>
{
    public void Configure(EntityTypeBuilder<Submission> b)
    {
        b.ToTable("submissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.Score).HasPrecision(10, 2);
        b.HasIndex(x => new { x.AssignmentId, x.StudentProfileId }).IsUnique();
    }
}

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Amount).HasPrecision(18, 2);
    }
}

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> b)
    {
        b.ToTable("conversations");
        b.HasKey(x => x.Id);
    }
}

public sealed class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> b)
    {
        b.ToTable("conversation_participants");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ConversationId, x.UserId }).IsUnique();
    }
}

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Text).IsRequired().HasMaxLength(4000);
    }
}

public sealed class BadgeConfiguration : IEntityTypeConfiguration<Badge>
{
    public void Configure(EntityTypeBuilder<Badge> b)
    {
        b.ToTable("badges");
        b.HasKey(x => x.Id);
        b.Property<string>("Code").HasMaxLength(64);
        b.HasIndex("Code").IsUnique();
    }
}

public sealed class StudentBadgeConfiguration : IEntityTypeConfiguration<StudentBadge>
{
    public void Configure(EntityTypeBuilder<StudentBadge> b)
    {
        b.ToTable("student_badges");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.BadgeId, x.StudentProfileId }).IsUnique();
    }
}

public sealed class XpLedgerConfiguration : IEntityTypeConfiguration<XpLedger>
{
    public void Configure(EntityTypeBuilder<XpLedger> b)
    {
        b.ToTable("xp_ledger");
        b.HasKey(x => x.Id);
    }
}

public sealed class ResultEntryConfiguration : IEntityTypeConfiguration<ResultEntry>
{
    public void Configure(EntityTypeBuilder<ResultEntry> b)
    {
        b.ToTable("results");
        b.HasKey(x => x.Id);
        b.Property(x => x.StudentFullName).IsRequired().HasMaxLength(256);
        b.Property(x => x.MainImageUrl).HasMaxLength(1024);
        b.Property(x => x.OverallScore).HasPrecision(10, 2);
        b.Property(x => x.Description).HasMaxLength(4000);
        b.Property(x => x.ImprovementText).HasMaxLength(1000);
        b.Property(x => x.DurationText).HasMaxLength(500);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.Property(x => x.BadgeIcon).HasMaxLength(512);
        b.Property(x => x.Language).IsRequired().HasMaxLength(10);
        b.HasIndex(x => new { x.IsPublished, x.IsFeatured, x.ExamType });
    }
}

public sealed class ResultScoreBreakdownConfiguration : IEntityTypeConfiguration<ResultScoreBreakdown>
{
    public void Configure(EntityTypeBuilder<ResultScoreBreakdown> b)
    {
        b.ToTable("result_score_breakdowns");
        b.HasKey(x => x.Id);
        b.Property(x => x.Key).IsRequired().HasMaxLength(128);
        b.Property(x => x.Value).IsRequired().HasMaxLength(128);
        b.HasOne(x => x.Result).WithMany(x => x.ScoreBreakdowns).HasForeignKey(x => x.ResultId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ResultId, x.Key }).IsUnique();
    }
}

public sealed class ResultImageConfiguration : IEntityTypeConfiguration<ResultImage>
{
    public void Configure(EntityTypeBuilder<ResultImage> b)
    {
        b.ToTable("result_images");
        b.HasKey(x => x.Id);
        b.Property(x => x.ImageUrl).IsRequired().HasMaxLength(1024);
        b.Property(x => x.ThumbnailUrl).HasMaxLength(1024);
        b.HasOne(x => x.Result).WithMany(x => x.Images).HasForeignKey(x => x.ResultId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class ResultViewConfiguration : IEntityTypeConfiguration<ResultView>
{
    public void Configure(EntityTypeBuilder<ResultView> b)
    {
        b.ToTable("result_views");
        b.HasKey(x => x.Id);
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.Property(x => x.UserAgent).HasMaxLength(1024);
        b.HasOne(x => x.Result).WithMany(x => x.Views).HasForeignKey(x => x.ResultId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.ResultId);
    }
}
