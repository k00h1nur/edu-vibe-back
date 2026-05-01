using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Badges;

public sealed record BadgeDto(Guid Id, string Name, int XpReward);

public sealed record BadgesPingCommand : IRequest<Result<string>>;

public sealed class BadgesPingCommandHandler : IRequestHandler<BadgesPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(BadgesPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Badges module ready"));
    }
}

public sealed record CreateBadgeCommand(string Name, int XpReward) : IRequest<Result<BadgeDto>>;

public sealed record UpdateBadgeCommand(Guid BadgeId, string Name, int XpReward) : IRequest<Result<BadgeDto>>;

public sealed record AwardBadgeCommand(Guid BadgeId, Guid StudentProfileId) : IRequest<Result>;

public sealed record GetStudentBadgesQuery(Guid StudentProfileId) : IRequest<Result<IReadOnlyCollection<BadgeDto>>>;