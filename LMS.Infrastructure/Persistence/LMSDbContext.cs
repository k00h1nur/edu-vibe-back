using LMS.Application.Common.Abstractions;
using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Persistence;

public sealed class LMSDbContext : DbContext, IApplicationDbContext
{
    public LMSDbContext(DbContextOptions<LMSDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<StaffProfile> StaffProfiles => Set<StaffProfile>();
    public DbSet<StudentProfile> StudentProfiles => Set<StudentProfile>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
    public DbSet<Attendance> Attendance => Set<Attendance>();
    public DbSet<Assignment> Assignments => Set<Assignment>();
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Badge> Badges => Set<Badge>();
    public DbSet<StudentBadge> StudentBadges => Set<StudentBadge>();
    public DbSet<XpLedger> XpLedger => Set<XpLedger>();
    public DbSet<ResultEntry> Results => Set<ResultEntry>();
    public DbSet<ResultScoreBreakdown> ResultScoreBreakdowns => Set<ResultScoreBreakdown>();
    public DbSet<ResultImage> ResultImages => Set<ResultImage>();
    public DbSet<ResultView> ResultViews => Set<ResultView>();
    public DbSet<VisitorMessage> VisitorMessages => Set<VisitorMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LMSDbContext).Assembly);
        SeedData.Apply(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }
}
