using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Dashboard;

public sealed class DashboardHandlers(IApplicationDbContext db, ICurrentUserService currentUser) :
    IRequestHandler<GetDirectorDashboardQuery, Result<DirectorDashboardDto>>,
    IRequestHandler<GetOfficeAdminDashboardQuery, Result<OfficeAdminDashboardDto>>,
    IRequestHandler<GetTeacherDashboardQuery, Result<TeacherDashboardDto>>,
    IRequestHandler<GetStudentDashboardQuery, Result<StudentDashboardDto>>
{
    public async Task<Result<DirectorDashboardDto>> Handle(GetDirectorDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var nowUtc = DateTime.UtcNow;
        var monthStart = new DateTime(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var dto = new DirectorDashboardDto(
            await db.StudentProfiles.CountAsync(cancellationToken),
            await db.UserRoles.Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur, r })
                .CountAsync(x => x.r.Code == "Teacher", cancellationToken),
            await db.Classes.CountAsync(x => x.Status == ClassStatus.Active, cancellationToken),
            await db.Payments.Where(x => x.Status == PaymentStatus.Paid && x.CreatedAt >= monthStart)
                .SumAsync(x => x.Amount, cancellationToken),
            await db.Attendance.CountAsync(cancellationToken));
        return Result<DirectorDashboardDto>.Ok(dto);
    }

    public async Task<Result<OfficeAdminDashboardDto>> Handle(GetOfficeAdminDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var dto = new OfficeAdminDashboardDto(
            await db.StudentProfiles.CountAsync(cancellationToken),
            await db.Enrollments.CountAsync(cancellationToken),
            await db.ClassSessions.CountAsync(x => !x.IsBackfilled, cancellationToken),
            await db.Payments.Where(x => x.Status == PaymentStatus.Pending).SumAsync(x => x.Amount, cancellationToken));
        return Result<OfficeAdminDashboardDto>.Ok(dto);
    }

    public async Task<Result<StudentDashboardDto>> Handle(GetStudentDashboardQuery request,
        CancellationToken cancellationToken)
    {
        // SECURITY: a student may read only their OWN dashboard counters. Staff
        // (StaffProfileId present) may read any student's for support workflows.
        if (currentUser.StaffProfileId is null &&
            (currentUser.StudentProfileId is null || currentUser.StudentProfileId != request.StudentProfileId))
        {
            return Result<StudentDashboardDto>.Fail("FORBIDDEN", "You can only view your own dashboard.");
        }

        var sp = await db.StudentProfiles.FirstOrDefaultAsync(x => x.Id == request.StudentProfileId, cancellationToken);
        if (sp is null) return Result<StudentDashboardDto>.Fail("NOT_FOUND", "Student profile not found.");
        var classIds = await db.Enrollments.Where(x => x.StudentProfileId == sp.Id).Select(x => x.ClassId)
            .ToListAsync(cancellationToken);
        var dto = new StudentDashboardDto(
            sp.XP,
            sp.Streak,
            await db.ClassSessions.CountAsync(x => classIds.Contains(x.ClassId) && !x.IsBackfilled, cancellationToken),
            await db.Assignments.CountAsync(x => classIds.Contains(x.ClassId) && x.Status != AssignmentStatus.Closed,
                cancellationToken),
            await db.StudentBadges.CountAsync(x => x.StudentProfileId == sp.Id, cancellationToken),
            await db.Submissions.CountAsync(x => x.StudentProfileId == sp.Id && x.Status == SubmissionStatus.Graded,
                cancellationToken));
        return Result<StudentDashboardDto>.Ok(dto);
    }

    public async Task<Result<TeacherDashboardDto>> Handle(GetTeacherDashboardQuery request,
        CancellationToken cancellationToken)
    {
        // SECURITY: a teacher may read only their OWN dashboard; admins/office may
        // read any teacher's. Without this any Dashboard.Teacher holder could read
        // another teacher's class/enrollment/grading/attendance counters.
        var isAdmin = currentUser.IsAdmin();
        if (!isAdmin && currentUser.UserId != request.TeacherUserId)
        {
            return Result<TeacherDashboardDto>.Fail("FORBIDDEN", "You can only view your own dashboard.");
        }

        var classIds = await db.Classes.Where(x => x.TeacherUserId == request.TeacherUserId).Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var dto = new TeacherDashboardDto(
            classIds.Count,
            await db.Enrollments.CountAsync(x => classIds.Contains(x.ClassId), cancellationToken),
            await db.Submissions.Join(db.Assignments, s => s.AssignmentId, a => a.Id, (s, a) => new { s, a })
                .CountAsync(x => classIds.Contains(x.a.ClassId) && x.s.Status != SubmissionStatus.Graded,
                    cancellationToken),
            await db.Attendance.CountAsync(x => classIds.Contains(x.ClassId), cancellationToken));
        return Result<TeacherDashboardDto>.Ok(dto);
    }
}
