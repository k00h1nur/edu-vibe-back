using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Attendance;

public sealed class AttendanceHandlers(IApplicationDbContext db) :
    IRequestHandler<MarkAttendanceCommand, Result<AttendanceDto>>,
    IRequestHandler<UpdateAttendanceCommand, Result<AttendanceDto>>,
    IRequestHandler<GetSessionAttendanceQuery, Result<IReadOnlyCollection<AttendanceDto>>>,
    IRequestHandler<GetStudentAttendanceQuery, Result<IReadOnlyCollection<AttendanceDto>>>
{
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

    private static AttendanceDto Map(Domain.Entities.Attendance a)
    {
        return new AttendanceDto(a.Id, a.ClassId, a.SessionId, a.StudentProfileId, a.Status);
    }
}