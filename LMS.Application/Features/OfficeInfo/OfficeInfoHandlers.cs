using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using DomainOfficeInfo = LMS.Domain.Entities.OfficeInfo;

namespace LMS.Application.Features.OfficeInfo;

public sealed class OfficeInfoHandlers(IApplicationDbContext db) :
    IRequestHandler<GetOfficeInfoQuery, Result<OfficeInfoDto>>,
    IRequestHandler<UpdateOfficeInfoCommand, Result<OfficeInfoDto>>
{
    public async Task<Result<OfficeInfoDto>> Handle(GetOfficeInfoQuery request, CancellationToken ct)
    {
        var info = await GetOrCreate(ct);
        return Result<OfficeInfoDto>.Ok(Map(info));
    }

    public async Task<Result<OfficeInfoDto>> Handle(UpdateOfficeInfoCommand request, CancellationToken ct)
    {
        var info = await GetOrCreate(ct);
        info.Update(
            request.AcademyName, request.Phone, request.Email, request.Address,
            request.ResultsContent, request.TeachersIntro, request.HeroContent,
            request.InstagramUrl, request.FacebookUrl, request.TelegramUrl,
            request.YoutubeUrl, request.TiktokUrl, request.LinkedInUrl);
        await db.SaveChangesAsync(ct);
        return Result<OfficeInfoDto>.Ok(Map(info));
    }

    private async Task<DomainOfficeInfo> GetOrCreate(CancellationToken ct)
    {
        var info = await db.OfficeInfos.FirstOrDefaultAsync(x => x.Id == DomainOfficeInfo.SingletonId, ct);
        if (info is null)
        {
            info = new DomainOfficeInfo(DomainOfficeInfo.SingletonId);
            await db.OfficeInfos.AddAsync(info, ct);
            await db.SaveChangesAsync(ct);
        }
        return info;
    }

    private static OfficeInfoDto Map(DomainOfficeInfo i) => new(
        i.AcademyName, i.Phone, i.Email, i.Address,
        i.ResultsContent, i.TeachersIntro, i.HeroContent,
        i.InstagramUrl, i.FacebookUrl, i.TelegramUrl,
        i.YoutubeUrl, i.TiktokUrl, i.LinkedInUrl);
}
