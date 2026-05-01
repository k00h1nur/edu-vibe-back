using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Sessions;

public sealed record SessionDto(
    Guid Id,
    Guid ClassId,
    DateOnly SessionDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? RoomId);

public sealed record SessionsPingCommand : IRequest<Result<string>>;

public sealed class SessionsPingCommandHandler : IRequestHandler<SessionsPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(SessionsPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Sessions module ready"));
    }
}

public sealed record CreateClassSessionCommand(
    Guid ClassId,
    DateOnly SessionDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? RoomId) : IRequest<Result<SessionDto>>;

public sealed record UpdateClassSessionCommand(
    Guid SessionId,
    DateOnly SessionDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? RoomId) : IRequest<Result<SessionDto>>;

public sealed record CancelClassSessionCommand(Guid SessionId) : IRequest<Result>;

public sealed record GetClassSessionsQuery(Guid ClassId) : IRequest<Result<IReadOnlyCollection<SessionDto>>>;

public sealed record GetMyScheduleQuery(Guid UserId) : IRequest<Result<IReadOnlyCollection<SessionDto>>>;