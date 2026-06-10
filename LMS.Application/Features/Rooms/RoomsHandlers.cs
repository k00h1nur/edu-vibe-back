using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Rooms;

public sealed class RoomsHandlers(IApplicationDbContext db) :
    IRequestHandler<GetRoomsQuery, Result<IReadOnlyCollection<RoomDto>>>,
    IRequestHandler<CreateRoomCommand, Result<RoomDto>>,
    IRequestHandler<UpdateRoomCommand, Result<RoomDto>>,
    IRequestHandler<DeleteRoomCommand, Result>
{
    public async Task<Result<RoomDto>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
    {
        if (await db.Rooms.AnyAsync(x => x.Name == request.Name, cancellationToken))
            return Result<RoomDto>.Fail("DUPLICATE", "Room name exists.");
        var r = new Room(request.Name, request.Capacity, request.MeetingLink);
        await db.Rooms.AddAsync(r, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<RoomDto>.Ok(new RoomDto(r.Id, r.Name, r.Capacity, r.MeetingLink));
    }

    public async Task<Result> Handle(DeleteRoomCommand request, CancellationToken cancellationToken)
    {
        var r = await db.Rooms.FirstOrDefaultAsync(x => x.Id == request.RoomId, cancellationToken);
        if (r is null) return Result.Fail("NOT_FOUND", "Room not found.");
        db.Rooms.Remove(r);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Deleted");
    }

    public async Task<Result<IReadOnlyCollection<RoomDto>>> Handle(GetRoomsQuery request,
        CancellationToken cancellationToken)
    {
        // Stable order + AsNoTracking — pure DTO projection so no entity
        // ever enters the change tracker.
        return Result<IReadOnlyCollection<RoomDto>>.Ok(await db.Rooms.AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new RoomDto(x.Id, x.Name, x.Capacity, x.MeetingLink))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<RoomDto>> Handle(UpdateRoomCommand request, CancellationToken cancellationToken)
    {
        var r = await db.Rooms.FirstOrDefaultAsync(x => x.Id == request.RoomId, cancellationToken);
        if (r is null) return Result<RoomDto>.Fail("NOT_FOUND", "Room not found.");
        r.Update(request.Name, request.Capacity, request.MeetingLink);
        await db.SaveChangesAsync(cancellationToken);
        return Result<RoomDto>.Ok(new RoomDto(r.Id, r.Name, r.Capacity, r.MeetingLink));
    }
}