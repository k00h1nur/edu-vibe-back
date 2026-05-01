using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Sessions;

public sealed class SessionsHandlers(IApplicationDbContext db) :
    IRequestHandler<CreateClassSessionCommand, Result<SessionDto>>,
    IRequestHandler<UpdateClassSessionCommand, Result<SessionDto>>,
    IRequestHandler<CancelClassSessionCommand, Result>,
    IRequestHandler<GetClassSessionsQuery, Result<IReadOnlyCollection<SessionDto>>>,
    IRequestHandler<GetMyScheduleQuery, Result<IReadOnlyCollection<SessionDto>>>
{
    public async Task<Result> Handle(CancelClassSessionCommand request, CancellationToken cancellationToken)
    {
        var s = await db.ClassSessions.FirstOrDefaultAsync(x => x.Id == request.SessionId, cancellationToken);
        if (s is null) return Result.Fail("NOT_FOUND", "Session not found.");
        db.ClassSessions.Remove(s);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Deleted");
    }

    public async Task<Result<SessionDto>> Handle(CreateClassSessionCommand request, CancellationToken cancellationToken)
    {
        var s = new ClassSession(request.ClassId, request.SessionDate, request.StartsAt, request.EndsAt,
            request.RoomId);
        await db.ClassSessions.AddAsync(s, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<SessionDto>.Ok(Map(s));
    }

    public async Task<Result<IReadOnlyCollection<SessionDto>>> Handle(GetClassSessionsQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<SessionDto>>.Ok(await db.ClassSessions
            .Where(x => x.ClassId == request.ClassId)
            .Select(x => new SessionDto(x.Id, x.ClassId, x.SessionDate, x.StartsAt, x.EndsAt, x.RoomId))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<IReadOnlyCollection<SessionDto>>> Handle(GetMyScheduleQuery request,
        CancellationToken cancellationToken)
    {
        var studentIds = await db.StudentProfiles.Where(x => x.UserId == request.UserId).Select(x => x.Id)
            .ToListAsync(cancellationToken);
        var classIds = await db.Enrollments.Where(x => studentIds.Contains(x.StudentProfileId)).Select(x => x.ClassId)
            .Distinct().ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<SessionDto>>.Ok(await db.ClassSessions
            .Where(x => classIds.Contains(x.ClassId))
            .Select(x => new SessionDto(x.Id, x.ClassId, x.SessionDate, x.StartsAt, x.EndsAt, x.RoomId))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<SessionDto>> Handle(UpdateClassSessionCommand request, CancellationToken cancellationToken)
    {
        var s = await db.ClassSessions.FirstOrDefaultAsync(x => x.Id == request.SessionId, cancellationToken);
        if (s is null) return Result<SessionDto>.Fail("NOT_FOUND", "Session not found.");
        typeof(ClassSession).GetProperty(nameof(ClassSession.SessionDate))!.SetValue(s, request.SessionDate);
        typeof(ClassSession).GetProperty(nameof(ClassSession.StartsAt))!.SetValue(s, request.StartsAt);
        typeof(ClassSession).GetProperty(nameof(ClassSession.EndsAt))!.SetValue(s, request.EndsAt);
        typeof(ClassSession).GetProperty(nameof(ClassSession.RoomId))!.SetValue(s, request.RoomId);
        await db.SaveChangesAsync(cancellationToken);
        return Result<SessionDto>.Ok(Map(s));
    }

    private static SessionDto Map(ClassSession s)
    {
        return new SessionDto(s.Id, s.ClassId, s.SessionDate, s.StartsAt, s.EndsAt, s.RoomId);
    }
}