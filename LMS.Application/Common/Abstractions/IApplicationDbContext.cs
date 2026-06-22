using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Common.Abstractions;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<StaffProfile> StaffProfiles { get; }
    DbSet<StudentProfile> StudentProfiles { get; }
    DbSet<Course> Courses { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Class> Classes { get; }
    DbSet<ClassResource> ClassResources { get; }
    DbSet<CurriculumTemplate> CurriculumTemplates { get; }
    DbSet<CurriculumModule> CurriculumModules { get; }
    DbSet<CurriculumUnit> CurriculumUnits { get; }
    DbSet<CurriculumLesson> CurriculumLessons { get; }
    DbSet<Enrollment> Enrollments { get; }
    DbSet<ClassSession> ClassSessions { get; }
    DbSet<ClassSchedulePattern> ClassSchedulePatterns { get; }
    DbSet<Attendance> Attendance { get; }
    DbSet<Assignment> Assignments { get; }
    DbSet<Submission> Submissions { get; }
    DbSet<SubmissionFile> SubmissionFiles { get; }
    DbSet<SubmissionAudit> SubmissionAudits { get; }
    DbSet<LessonMaterial> LessonMaterials { get; }
    DbSet<LessonProgress> LessonProgress { get; }
    DbSet<Payment> Payments { get; }
    DbSet<Conversation> Conversations { get; }
    DbSet<ConversationParticipant> ConversationParticipants { get; }
    DbSet<Message> Messages { get; }
    DbSet<Badge> Badges { get; }
    DbSet<StudentBadge> StudentBadges { get; }
    DbSet<XpLedger> XpLedger { get; }
    DbSet<ResultEntry> Results { get; }
    DbSet<ResultScoreBreakdown> ResultScoreBreakdowns { get; }
    DbSet<ResultImage> ResultImages { get; }
    DbSet<ResultView> ResultViews { get; }
    DbSet<VisitorMessage> VisitorMessages { get; }
    DbSet<Book> Books { get; }
    DbSet<AssignmentBook> AssignmentBooks { get; }
    DbSet<AssignmentAssignee> AssignmentAssignees { get; }
    DbSet<LearningTask> LearningTasks { get; }
    DbSet<TaskSubmission> TaskSubmissions { get; }
    DbSet<Reminder> Reminders { get; }
    DbSet<Specialization> Specializations { get; }
    DbSet<StaffSpecialization> StaffSpecializations { get; }
    DbSet<Material> Materials { get; }
    DbSet<MaterialClass> MaterialClasses { get; }
    DbSet<OfficeInfo> OfficeInfo { get; }
    DbSet<Announcement> Announcements { get; }
    DbSet<MarketingCourse> MarketingCourses { get; }
    DbSet<MarketingVideo> MarketingVideos { get; }
    DbSet<MockTestSlot> MockTestSlots { get; }
    DbSet<TelegramAccount> TelegramAccounts { get; }
    DbSet<TelegramSettings> TelegramSettings { get; }
    DbSet<TelegramDeepLinkToken> TelegramDeepLinkTokens { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
