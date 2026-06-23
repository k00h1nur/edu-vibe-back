using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OfficeInfoEntity = LMS.Domain.Entities.OfficeInfo;

namespace LMS.Application.Features.OfficeInfo;

/// <summary>
/// Singleton handler — Get returns the one row, Upsert creates it on the
/// first save and updates it thereafter. Both share the canonical singleton
/// id so the contract stays deterministic across deploys.
/// </summary>
public sealed class OfficeInfoHandlers(IApplicationDbContext db) :
    IRequestHandler<GetOfficeInfoQuery, Result<OfficeInfoDto>>,
    IRequestHandler<UpsertOfficeInfoCommand, Result<OfficeInfoDto>>
{
    public async Task<Result<OfficeInfoDto>> Handle(GetOfficeInfoQuery request, CancellationToken ct)
    {
        var row = await db.OfficeInfo.AsNoTracking().FirstOrDefaultAsync(ct);
        if (row is null)
        {
            // Return a placeholder so the marketing site never sees a 404
            // before the admin has filled it in. Empty values are nicer to
            // render than "missing".
            var empty = new OfficeInfoDto(
                AcademyName: "EduVibe Academy",
                Tagline: null, PhoneNumber: null, SecondaryPhone: null,
                Email: null, Address: null, WorkingHours: null,
                TelegramUrl: null, InstagramUrl: null, FacebookUrl: null,
                YoutubeUrl: null, WebsiteUrl: null, AboutHtml: null,
                MapEmbedUrl: null,
                UpdatedAt: DateTime.UtcNow);
            return Result<OfficeInfoDto>.Ok(empty);
        }
        return Result<OfficeInfoDto>.Ok(Map(row));
    }

    public async Task<Result<OfficeInfoDto>> Handle(UpsertOfficeInfoCommand request, CancellationToken ct)
    {
        var row = await db.OfficeInfo.FirstOrDefaultAsync(ct);
        if (row is null)
        {
            row = new OfficeInfoEntity(
                request.AcademyName, request.Tagline,
                request.PhoneNumber, request.SecondaryPhone,
                request.Email, request.Address, request.WorkingHours,
                request.TelegramUrl, request.InstagramUrl, request.FacebookUrl,
                request.YoutubeUrl, request.WebsiteUrl, request.AboutHtml, request.MapEmbedUrl);
            await db.OfficeInfo.AddAsync(row, ct);
        }
        else
        {
            row.Update(
                request.AcademyName, request.Tagline,
                request.PhoneNumber, request.SecondaryPhone,
                request.Email, request.Address, request.WorkingHours,
                request.TelegramUrl, request.InstagramUrl, request.FacebookUrl,
                request.YoutubeUrl, request.WebsiteUrl, request.AboutHtml, request.MapEmbedUrl);
        }
        await db.SaveChangesAsync(ct);
        return Result<OfficeInfoDto>.Ok(Map(row), "Saved");
    }

    private static OfficeInfoDto Map(OfficeInfoEntity r) => new(
        r.AcademyName, r.Tagline, r.PhoneNumber, r.SecondaryPhone, r.Email,
        r.Address, r.WorkingHours, r.TelegramUrl, r.InstagramUrl, r.FacebookUrl,
        r.YoutubeUrl, r.WebsiteUrl, r.AboutHtml, r.MapEmbedUrl, r.UpdatedAt);
}
