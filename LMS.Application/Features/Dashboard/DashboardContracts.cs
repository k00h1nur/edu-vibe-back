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
    int GradedSubmissions);

public sealed record DashboardPingCommand : IRequest<Result<string>>;

public sealed class DashboardPingCommandHandler : IRequestHandler<DashboardPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(DashboardPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Dashboard module ready"));
    }
}

public sealed record GetDirectorDashboardQuery : IRequest<Result<DirectorDashboardDto>>;

public sealed record GetOfficeAdminDashboardQuery : IRequest<Result<OfficeAdminDashboardDto>>;

public sealed record GetTeacherDashboardQuery(Guid TeacherUserId) : IRequest<Result<TeacherDashboardDto>>;

public sealed record GetStudentDashboardQuery(Guid StudentProfileId) : IRequest<Result<StudentDashboardDto>>;