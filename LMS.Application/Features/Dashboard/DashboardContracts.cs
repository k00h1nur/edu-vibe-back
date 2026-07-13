using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Dashboard;

public sealed record DirectorDashboardDto(
    int TotalStudents,
    int TotalTeachers,
    int ActiveClasses,
    decimal MonthlyRevenue,
    int AttendanceCount);

public sealed record OfficeAdminDashboardDto(
    int StudentCount,
    int EnrollmentCount,
    int SessionCount,
    decimal PaymentsPending);

public sealed record TeacherDashboardDto(
    int OwnClasses,
    int AssignedStudents,
    int PendingSubmissions,
    int AttendanceCount);

public sealed record StudentDashboardDto(
    int CurrentXp,
    int StreakDays,
    int UpcomingSessions,
    int PendingAssignments,
    int EarnedBadges,
    int GradedSubmissions,
    // Numeric game level derived from CurrentXp, plus progress into it (XP earned
    // toward the next level / the level's total span) for a progress ring.
    int Level,
    int XpIntoLevel,
    int XpForNextLevel);

public sealed record GetDirectorDashboardQuery : IRequest<Result<DirectorDashboardDto>>;

public sealed record GetOfficeAdminDashboardQuery : IRequest<Result<OfficeAdminDashboardDto>>;

public sealed record GetTeacherDashboardQuery(Guid TeacherUserId) : IRequest<Result<TeacherDashboardDto>>;

public sealed record GetStudentDashboardQuery(Guid StudentProfileId) : IRequest<Result<StudentDashboardDto>>;