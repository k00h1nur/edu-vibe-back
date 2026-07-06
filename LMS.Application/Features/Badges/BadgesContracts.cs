using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Badges;

public sealed record BadgeDto(Guid Id, string Name, int XpReward);

public sealed record CreateBadgeCommand(string Name, int XpReward) : IRequest<Result<BadgeDto>>;

public sealed record UpdateBadgeCommand(Guid BadgeId, string Name, int XpReward) : IRequest<Result<BadgeDto>>;

public sealed record AwardBadgeCommand(Guid BadgeId, Guid StudentProfileId) : IRequest<Result>;

public sealed record GetStudentBadgesQuery(Guid StudentProfileId) : IRequest<Result<IReadOnlyCollection<BadgeDto>>>;