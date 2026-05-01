using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Rooms;

public sealed record RoomDto(Guid Id, string Name, int Capacity, string? MeetingLink);

public sealed record RoomsPingCommand : IRequest<Result<string>>;

public sealed class RoomsPingCommandHandler : IRequestHandler<RoomsPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(RoomsPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Rooms module ready"));
    }
}

public sealed record CreateRoomCommand(string Name, int Capacity, string? MeetingLink) : IRequest<Result<RoomDto>>;

public sealed record UpdateRoomCommand(Guid RoomId, string Name, int Capacity, string? MeetingLink)
    : IRequest<Result<RoomDto>>;

public sealed record GetRoomsQuery : IRequest<Result<IReadOnlyCollection<RoomDto>>>;

public sealed record DeleteRoomCommand(Guid RoomId) : IRequest<Result>;