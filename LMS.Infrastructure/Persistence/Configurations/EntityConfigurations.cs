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

        // RefreshTokenCommandHandler does a hash equality lookup as the first
        // filter. Indexing the hash + expiry lets PostgreSQL skip every user
        // whose token is null or expired without a table scan.
        b.HasIndex(x => new { x.RefreshTokenHash, x.RefreshTokenExpiresAt })
            .HasDatabaseName("ix_users_refresh_token_lookup");
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
        // Unique pair (UserId, RoleId) is already covered. The PermissionAuthorizationHandler
        // and LoginCommandHandler filter on UserId alone — needs its own index.
        b.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();
        b.HasIndex(x => x.UserId).HasDatabaseName("ix_user_roles_user_id");
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
        // Used by Classes/assigned/{teacherUserId} + ResolveUserClassIds in the
        // session handlers. EF doesn't auto-index FKs on optional one-to-many.
        b.HasIndex(x => x.TeacherUserId).HasDatabaseName("ix_classes_teacher_user_id");
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
        // The composite covers WHERE ClassId = X via left-prefix. Filtering by
        // StudentProfileId alone (assignments lookups, dashboards) needs its own index.
        b.HasIndex(x => x.StudentProfileId).HasDatabaseName("ix_enrollments_student_profile_id");
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
        // GET /api/Attendance and the student-history query filter by single columns.
        b.HasIndex(x => x.SessionId).HasDatabaseName("ix_attendance_session_id");
        b.HasIndex(x => x.StudentProfileId).HasDatabaseName("ix_attendance_student_profile_id");
        b.HasIndex(x => x.ClassId).HasDatabaseName("ix_attendance_class_id");
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
        // Conversations/my/{userId} and unread-count both filter by UserId only.
        b.HasIndex(x => x.UserId).HasDatabaseName("ix_conversation_participants_user_id");
    }
}

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> b)
    {
        b.ToTable("messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Text).IsRequired().HasMaxLength(4000);
        // Hot paths: list messages by conversation, count unread per user.
        b.HasIndex(x => new { x.ConversationId, x.ReadAt })
            .HasDatabaseName("ix_messages_conversation_read_at");
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

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> b)
    {
        b.ToTable("books");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).IsRequired().HasMaxLength(256);
        b.Property(x => x.Author).HasMaxLength(256);
        b.Property(x => x.Subject).HasMaxLength(64);
        b.Property(x => x.Level).HasMaxLength(32);
        b.Property(x => x.Description).HasMaxLength(2000);
        b.Property(x => x.CoverImageUrl).HasMaxLength(1024);
        b.Property(x => x.FileUrl).HasMaxLength(1024);
        b.HasIndex(x => x.Title);
        b.HasIndex(x => x.Subject);
    }
}

public sealed class AssignmentBookConfiguration : IEntityTypeConfiguration<AssignmentBook>
{
    public void Configure(EntityTypeBuilder<AssignmentBook> b)
    {
        b.ToTable("assignment_books");
        b.HasKey(x => x.Id);
        b.Property(x => x.Note).HasMaxLength(512);
        b.HasIndex(x => new { x.AssignmentId, x.BookId }).IsUnique();
        b.HasOne(x => x.Assignment).WithMany().HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Book).WithMany(x => x.AssignmentLinks).HasForeignKey(x => x.BookId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class AssignmentAssigneeConfiguration : IEntityTypeConfiguration<AssignmentAssignee>
{
    public void Configure(EntityTypeBuilder<AssignmentAssignee> b)
    {
        b.ToTable("assignment_assignees");
        b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.AssignmentId, x.StudentProfileId }).IsUnique();
        b.HasIndex(x => x.StudentProfileId).HasDatabaseName("ix_assignment_assignees_student_id");
        b.HasOne(x => x.Assignment).WithMany().HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.StudentProfile).WithMany().HasForeignKey(x => x.StudentProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class LearningTaskConfiguration : IEntityTypeConfiguration<LearningTask>
{
    public void Configure(EntityTypeBuilder<LearningTask> b)
    {
        b.ToTable("learning_tasks");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).IsRequired().HasMaxLength(256);
        // jsonb on PostgreSQL — efficient nested-path queries + smaller storage.
        b.Property(x => x.ContentJson).IsRequired().HasColumnType("jsonb");
        b.Property(x => x.SolutionJson).HasColumnType("jsonb");
        b.HasIndex(x => new { x.AssignmentId, x.Order })
            .HasDatabaseName("ix_learning_tasks_assignment_order");
        b.HasOne(x => x.Assignment).WithMany().HasForeignKey(x => x.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class TaskSubmissionConfiguration : IEntityTypeConfiguration<TaskSubmission>
{
    public void Configure(EntityTypeBuilder<TaskSubmission> b)
    {
        b.ToTable("task_submissions");
        b.HasKey(x => x.Id);
        b.Property(x => x.ResponseJson).IsRequired().HasColumnType("jsonb");
        b.Property(x => x.Score).HasPrecision(5, 4);
        b.Property(x => x.TeacherFeedback).HasMaxLength(2000);
        b.HasIndex(x => new { x.TaskId, x.StudentProfileId }).IsUnique();
        b.HasIndex(x => x.StudentProfileId).HasDatabaseName("ix_task_submissions_student_id");
        b.HasOne(x => x.Task).WithMany(x => x.Submissions).HasForeignKey(x => x.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.StudentProfile).WithMany().HasForeignKey(x => x.StudentProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class VisitorMessageConfiguration : IEntityTypeConfiguration<VisitorMessage>
{
    public void Configure(EntityTypeBuilder<VisitorMessage> b)
    {
        b.ToTable("visitor_messages");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        b.Property(x => x.Phone).IsRequired().HasMaxLength(64);
        b.Property(x => x.Email).HasMaxLength(320);
        b.Property(x => x.Message).IsRequired().HasMaxLength(4000);
        b.Property(x => x.Course).HasMaxLength(128);
        b.Property(x => x.PreferredTime).HasMaxLength(128);
        b.Property(x => x.Language).HasMaxLength(8);
        // Admin inbox sorts by CreatedAt desc and filters by IsRead.
        b.HasIndex(x => new { x.IsRead, x.CreatedAt }).HasDatabaseName("ix_visitor_messages_inbox");
    }
}


public sealed class ReminderConfiguration : IEntityTypeConfiguration<Reminder>
{
    public void Configure(EntityTypeBuilder<Reminder> b)
    {
        b.ToTable("reminders");
        b.HasKey(x => x.Id);
        b.Property(x => x.Title).IsRequired().HasMaxLength(256);
        b.Property(x => x.Notes).HasMaxLength(2000);
        b.HasOne(x => x.OwnerUser).WithMany().HasForeignKey(x => x.OwnerUserId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OwnerUserId, x.DueAt }).HasDatabaseName("ix_reminders_owner_due");
        b.HasIndex(x => new { x.OwnerUserId, x.IsCompleted }).HasDatabaseName("ix_reminders_owner_status");
public sealed class SpecializationConfiguration : IEntityTypeConfiguration<Specialization>
{
    public void Configure(EntityTypeBuilder<Specialization> b)
    {
        b.ToTable("specializations");
        b.HasKey(x => x.Id);
        b.Property(x => x.Code).IsRequired().HasMaxLength(64);
        b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        b.Property(x => x.IsActive).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class StaffSpecializationConfiguration : IEntityTypeConfiguration<StaffSpecialization>
{
    public void Configure(EntityTypeBuilder<StaffSpecialization> b)
    {
        b.ToTable("staff_specializations");
        b.HasKey(x => x.Id);
        b.HasOne(x => x.StaffProfile)
            .WithMany(x => x.Specializations)
            .HasForeignKey(x => x.StaffProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Specialization)
            .WithMany(x => x.StaffLinks)
            .HasForeignKey(x => x.SpecializationId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.StaffProfileId, x.SpecializationId }).IsUnique();
    }
}
