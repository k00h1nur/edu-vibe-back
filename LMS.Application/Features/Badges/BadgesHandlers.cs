using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Badges;

public sealed class BadgesHandlers(IApplicationDbContext db) :
    IRequestHandler<CreateBadgeCommand, Result<BadgeDto>>,
    IRequestHandler<UpdateBadgeCommand, Result<BadgeDto>>,
    IRequestHandler<AwardBadgeCommand, Result>,
    IRequestHandler<GetStudentBadgesQuery, Result<IReadOnlyCollection<BadgeDto>>>
{
    public async Task<Result> Handle(AwardBadgeCommand request, CancellationToken cancellationToken)
    {
        var badge = await db.Badges.FirstOrDefaultAsync(x => x.Id == request.BadgeId, cancellationToken);
        var sp = await db.StudentProfiles.FirstOrDefaultAsync(x => x.Id == request.StudentProfileId, cancellationToken);
        if (badge is null) return Result.Fail("NOT_FOUND", "Badge not found.");
        if (sp is null) return Result.Fail("NOT_FOUND", "Student profile not found.");

        var alreadyAwarded = await db.StudentBadges.AnyAsync(
            x => x.StudentProfileId == request.StudentProfileId && x.BadgeId == request.BadgeId,
            cancellationToken);
        if (alreadyAwarded) return Result.Fail("ALREADY_AWARDED", "Student already has this badge.");

        var sb = StudentBadge.Award(request.StudentProfileId, request.BadgeId, Array.Empty<StudentBadge>());
        await db.StudentBadges.AddAsync(sb, cancellationToken);

        if (badge.XpReward > 0)
        {
            sp.AddXp(badge.XpReward);
            await db.XpLedger.AddAsync(
                XpLedger.CreateEntry(sp.Id, badge.XpReward, XpSourceType.Manual, $"Badge:{badge.Name}"),
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Awarded");
    }

    public async Task<Result<BadgeDto>> Handle(CreateBadgeCommand request, CancellationToken cancellationToken)
    {
        var b = new Badge(request.Name, request.XpReward);
        await db.Badges.AddAsync(b, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<BadgeDto>.Ok(new BadgeDto(b.Id, b.Name, b.XpReward));
    }

    public async Task<Result<IReadOnlyCollection<BadgeDto>>> Handle(GetStudentBadgesQuery request,
        CancellationToken cancellationToken)
    {
        var ids = await db.StudentBadges.Where(x => x.StudentProfileId == request.StudentProfileId)
            .Select(x => x.BadgeId).ToListAsync(cancellationToken);
        var list = await db.Badges.Where(x => ids.Contains(x.Id)).Select(x => new BadgeDto(x.Id, x.Name, x.XpReward))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<BadgeDto>>.Ok(list);
    }

    public async Task<Result<BadgeDto>> Handle(UpdateBadgeCommand request, CancellationToken cancellationToken)
    {
        var b = await db.Badges.FirstOrDefaultAsync(x => x.Id == request.BadgeId, cancellationToken);
        if (b is null) return Result<BadgeDto>.Fail("NOT_FOUND", "Badge not found.");
        typeof(Badge).GetProperty(nameof(Badge.Name))!.SetValue(b, request.Name.Trim());
        typeof(Badge).GetProperty(nameof(Badge.XpReward))!.SetValue(b, request.XpReward);
        await db.SaveChangesAsync(cancellationToken);
        return Result<BadgeDto>.Ok(new BadgeDto(b.Id, b.Name, b.XpReward));
    }
}
