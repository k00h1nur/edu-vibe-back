using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Attendance;

public sealed class AttendanceHandlers(IApplicationDbContext db, ICurrentUserService currentUser) :
    IRequestHandler<MarkAttendanceCommand, Result<AttendanceDto>>,
    IRequestHandler<UpdateAttendanceCommand, Result<AttendanceDto>>,
    IRequestHandler<GetSessionAttendanceQuery, Result<IReadOnlyCollection<AttendanceDto>>>,
    IRequestHandler<GetStudentAttendanceQuery, Result<IReadOnlyCollection<AttendanceDto>>>,
    IRequestHandler<GetAttendanceQuery, Result<IReadOnlyCollection<AttendanceDto>>>
{
    // Roles allowed to read ANY student's attendance. Students hold
    // Attendance.Read too (for their own history), so the permission gate
    // alone can't tell "my attendance" from "someone else's".
    private static readonly string[] StaffRoles =
        { "admin", "superadmin", "teacher", "support_teacher", "office_admin", "academy_director" };

    private bool CallerIsStaff() => StaffRoles.Any(currentUser.IsInRole);

    public async Task<Result<IReadOnlyCollection<AttendanceDto>>> Handle(GetSessionAttendanceQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<AttendanceDto>>.Ok(await db.Attendance
            .Where(x => x.SessionId == request.SessionId)
            .Select(a => new AttendanceDto(a.Id, a.ClassId, a.SessionId, a.StudentProfileId, a.Status))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<IReadOnlyCollection<AttendanceDto>>> Handle(GetStudentAttendanceQuery request,
        CancellationToken cancellationToken)
    {
        // Self-scope: a non-staff caller (i.e. a student) may only read their
        // OWN attendance. Resolve their profile from the JWT claim, falling
        // back to a UserId → StudentProfiles lookup if the claim is absent.
        if (!CallerIsStaff())
        {
            var ownProfileId = currentUser.StudentProfileId;
            if (ownProfileId is null && currentUser.UserId is { } uid)
            {
                ownProfileId = await db.StudentProfiles
                    .Where(sp => sp.UserId == uid)
                    .Select(sp => (Guid?)sp.Id)
                    .FirstOrDefaultAsync(cancellationToken);
            }
            if (ownProfileId is null || ownProfileId.Value != request.StudentProfileId)
                return Result<IReadOnlyCollection<AttendanceDto>>.Fail(
                    "FORBIDDEN", "You can only view your own attendance.");
        }

        return Result<IReadOnlyCollection<AttendanceDto>>.Ok(await db.Attendance
            .Where(x => x.StudentProfileId == request.StudentProfileId)
            .Select(a => new AttendanceDto(a.Id, a.ClassId, a.SessionId, a.StudentProfileId, a.Status))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<AttendanceDto>> Handle(MarkAttendanceCommand request, CancellationToken cancellationToken)
    {
        var existing = await db.Attendance.FirstOrDefaultAsync(
            x => x.SessionId == request.SessionId && x.StudentProfileId == request.StudentProfileId, cancellationToken);
        if (existing is null)
        {
            var list = await db.Attendance.Where(x => x.SessionId == request.SessionId).ToListAsync(cancellationToken);
            existing = Domain.Entities.Attendance.Create(request.ClassId, request.SessionId, request.StudentProfileId,
                list);
            await db.Attendance.AddAsync(existing, cancellationToken);
        }

        existing.Mark(request.Status);
        await db.SaveChangesAsync(cancellationToken);
        return Result<AttendanceDto>.Ok(Map(existing));
    }

    public async Task<Result<AttendanceDto>> Handle(UpdateAttendanceCommand request,
        CancellationToken cancellationToken)
    {
        var a = await db.Attendance.FirstOrDefaultAsync(x => x.Id == request.AttendanceId, cancellationToken);
        if (a is null) return Result<AttendanceDto>.Fail("NOT_FOUND", "Attendance not found.");
        a.Mark(request.Status);
        await db.SaveChangesAsync(cancellationToken);
        return Result<AttendanceDto>.Ok(Map(a));
    }

    public async Task<Result<IReadOnlyCollection<AttendanceDto>>> Handle(GetAttendanceQuery request,
        CancellationToken cancellationToken)
    {
        var q = db.Attendance.AsQueryable();
        if (request.ClassId is { } classId) q = q.Where(x => x.ClassId == classId);
        if (request.SessionId is { } sessionId) q = q.Where(x => x.SessionId == sessionId);
        if (request.StudentProfileId is { } studentId) q = q.Where(x => x.StudentProfileId == studentId);
        if (request.Status is { } status) q = q.Where(x => x.Status == status);

        var data = await q
            .OrderByDescending(x => x.CreatedAt)
            .Select(a => new AttendanceDto(a.Id, a.ClassId, a.SessionId, a.StudentProfileId, a.Status))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<AttendanceDto>>.Ok(data);
    }

    private static AttendanceDto Map(Domain.Entities.Attendance a)
    {
        return new AttendanceDto(a.Id, a.ClassId, a.SessionId, a.StudentProfileId, a.Status);
    }
}