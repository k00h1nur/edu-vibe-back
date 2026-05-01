using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Xp;

public sealed record XpLedgerDto(
    Guid Id,
    Guid StudentProfileId,
    int Amount,
    string SourceType,
    string? Note,
    DateTime CreatedAt);

public sealed record LeaderboardDto(Guid StudentProfileId, int Xp);

public sealed record XpPingCommand : IRequest<Result<string>>;

public sealed class XpPingCommandHandler : IRequestHandler<XpPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(XpPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Xp module ready"));
    }
}

public sealed record AddManualXpCommand(Guid StudentProfileId, int Amount, string? Note) : IRequest<Result>;

public sealed record GetStudentXpLedgerQuery(Guid StudentProfileId)
    : IRequest<Result<IReadOnlyCollection<XpLedgerDto>>>;

public sealed record GetLeaderboardQuery(int Top = 10) : IRequest<Result<IReadOnlyCollection<LeaderboardDto>>>;