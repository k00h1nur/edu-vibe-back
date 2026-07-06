using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Rooms;

public sealed record RoomDto(Guid Id, string Name, int Capacity, string? MeetingLink);

public sealed record CreateRoomCommand(string Name, int Capacity, string? MeetingLink) : IRequest<Result<RoomDto>>;

public sealed record UpdateRoomCommand(Guid RoomId, string Name, int Capacity, string? MeetingLink)
    : IRequest<Result<RoomDto>>;

public sealed record GetRoomsQuery : IRequest<Result<IReadOnlyCollection<RoomDto>>>;

public sealed record DeleteRoomCommand(Guid RoomId) : IRequest<Result>;