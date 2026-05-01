using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Xp;

public sealed class XpHandlers(IApplicationDbContext db) :
    IRequestHandler<AddManualXpCommand, Result>,
    IRequestHandler<GetStudentXpLedgerQuery, Result<IReadOnlyCollection<XpLedgerDto>>>,
    IRequestHandler<GetLeaderboardQuery, Result<IReadOnlyCollection<LeaderboardDto>>>
{
    public async Task<Result> Handle(AddManualXpCommand request, CancellationToken cancellationToken)
    {
        var sp = await db.StudentProfiles.FirstOrDefaultAsync(x => x.Id == request.StudentProfileId, cancellationToken);
        if (sp is null) return Result.Fail("NOT_FOUND", "Student profile not found.");
        sp.AddXp(request.Amount);
        await db.XpLedger.AddAsync(XpLedger.CreateEntry(sp.Id, request.Amount, XpSourceType.Manual, request.Note),
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("XP added");
    }

    public async Task<Result<IReadOnlyCollection<LeaderboardDto>>> Handle(GetLeaderboardQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<LeaderboardDto>>.Ok(await db.StudentProfiles.OrderByDescending(x => x.XP)
            .Take(request.Top).Select(x => new LeaderboardDto(x.Id, x.XP)).ToListAsync(cancellationToken));
    }

    public async Task<Result<IReadOnlyCollection<XpLedgerDto>>> Handle(GetStudentXpLedgerQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<XpLedgerDto>>.Ok(await db.XpLedger
            .Where(x => x.StudentProfileId == request.StudentProfileId).OrderByDescending(x => x.CreatedAt).Select(x =>
                new XpLedgerDto(x.Id, x.StudentProfileId, x.Amount, x.SourceType.ToString(), x.Note, x.CreatedAt))
            .ToListAsync(cancellationToken));
    }
}