using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.MarketingCms;

public sealed class MarketingVideosHandlers(IApplicationDbContext db) :
    IRequestHandler<GetMarketingVideosQuery, Result<IReadOnlyCollection<MarketingVideoDto>>>,
    IRequestHandler<GetPublicMarketingVideosQuery, Result<IReadOnlyCollection<MarketingVideoDto>>>,
    IRequestHandler<CreateMarketingVideoCommand, Result<MarketingVideoDto>>,
    IRequestHandler<UpdateMarketingVideoCommand, Result<MarketingVideoDto>>,
    IRequestHandler<DeleteMarketingVideoCommand, Result>
{
    public async Task<Result<IReadOnlyCollection<MarketingVideoDto>>> Handle(
        GetMarketingVideosQuery request, CancellationToken ct)
    {
        var q = db.MarketingVideos.AsNoTracking();
        if (request.OnlyActive) q = q.Where(v => v.IsActive);
        var items = await q
            .OrderBy(v => v.SortOrder).ThenBy(v => v.Title)
            .Select(Project()).ToListAsync(ct);
        return Result<IReadOnlyCollection<MarketingVideoDto>>.Ok(items);
    }

    public async Task<Result<IReadOnlyCollection<MarketingVideoDto>>> Handle(
        GetPublicMarketingVideosQuery request, CancellationToken ct)
    {
        var items = await db.MarketingVideos.AsNoTracking()
            .Where(v => v.IsActive)
            .OrderBy(v => v.SortOrder).ThenBy(v => v.Title)
            .Select(Project()).ToListAsync(ct);
        return Result<IReadOnlyCollection<MarketingVideoDto>>.Ok(items);
    }

    public async Task<Result<MarketingVideoDto>> Handle(CreateMarketingVideoCommand request, CancellationToken ct)
    {
        var v = new MarketingVideo(
            request.Title, request.Description, request.VideoUrl, request.ThumbnailUrl,
            request.SortOrder, request.IsActive);
        await db.MarketingVideos.AddAsync(v, ct);
        await db.SaveChangesAsync(ct);
        return Result<MarketingVideoDto>.Ok(Map(v));
    }

    public async Task<Result<MarketingVideoDto>> Handle(UpdateMarketingVideoCommand request, CancellationToken ct)
    {
        var v = await db.MarketingVideos.FirstOrDefaultAsync(x => x.Id == request.VideoId, ct);
        if (v is null) return Result<MarketingVideoDto>.Fail("NOT_FOUND", "Video not found.");
        v.Update(request.Title, request.Description, request.VideoUrl, request.ThumbnailUrl,
            request.SortOrder, request.IsActive);
        await db.SaveChangesAsync(ct);
        return Result<MarketingVideoDto>.Ok(Map(v));
    }

    public async Task<Result> Handle(DeleteMarketingVideoCommand request, CancellationToken ct)
    {
        var v = await db.MarketingVideos.FirstOrDefaultAsync(x => x.Id == request.VideoId, ct);
        if (v is null) return Result.Fail("NOT_FOUND", "Video not found.");
        db.MarketingVideos.Remove(v);
        await db.SaveChangesAsync(ct);
        return Result.Ok("Deleted");
    }

    private static System.Linq.Expressions.Expression<Func<MarketingVideo, MarketingVideoDto>> Project()
        => v => new MarketingVideoDto(
            v.Id, v.Title, v.Description, v.VideoUrl, v.ThumbnailUrl, v.SortOrder, v.IsActive);

    private static MarketingVideoDto Map(MarketingVideo v) => new(
        v.Id, v.Title, v.Description, v.VideoUrl, v.ThumbnailUrl, v.SortOrder, v.IsActive);
}
