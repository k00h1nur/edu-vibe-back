using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Results;

public sealed class ResultsHandlers(
    IApplicationDbContext db,
    IResultImageService imageService)
    : IRequestHandler<ResultListQuery, Result<IReadOnlyCollection<ResultDto>>>,
      IRequestHandler<ResultByIdQuery, Result<ResultDto>>,
      IRequestHandler<FeaturedResultsQuery, Result<IReadOnlyCollection<ResultDto>>>,
      IRequestHandler<CreateResultCommand, Result<ResultDto>>,
      IRequestHandler<UpdateResultCommand, Result<ResultDto>>,
      IRequestHandler<DeleteResultCommand, Result>,
      IRequestHandler<UploadResultImageCommand, Result<ResultImageDto>>,
      IRequestHandler<ReplaceResultImageCommand, Result<ResultImageDto>>,
      IRequestHandler<DeleteResultImageCommand, Result>,
      IRequestHandler<ResultsAdminStatsQuery, Result<ResultsAdminStatsDto>>
{
    private async Task<ResultDto> Map(ResultEntry r, CancellationToken ct)
    {
        var b = await db.ResultScoreBreakdowns.Where(x => x.ResultId == r.Id && !x.IsDeleted)
            .Select(x => new ScoreBreakdownItemDto(x.Key, x.Value)).ToListAsync(ct);
        var imgs = await db.ResultImages.Where(x => x.ResultId == r.Id && !x.IsDeleted)
            .Select(x => new ResultImageDto(x.Id, x.ImageUrl, x.ThumbnailUrl, x.IsMain)).ToListAsync(ct);
        return new ResultDto(r.Id, r.StudentFullName, r.MainImageUrl, r.ExamType, r.OverallScore, r.Description,
            r.ImprovementText, r.DurationText, r.Notes, r.BadgeIcon, r.DisplayOrder, r.IsFeatured, r.IsPublished,
            r.Language, r.CreatedAt, r.ViewsCount, b, imgs);
    }

    public async Task<Result<IReadOnlyCollection<ResultDto>>> Handle(ResultListQuery q, CancellationToken ct)
    {
        var query = db.Results.Where(x => !x.IsDeleted && x.IsPublished);
        if (!string.IsNullOrWhiteSpace(q.Search)) query = query.Where(x => x.StudentFullName.Contains(q.Search));
        if (q.ExamType.HasValue) query = query.Where(x => x.ExamType == q.ExamType.Value);
        if (q.Featured.HasValue) query = query.Where(x => x.IsFeatured == q.Featured.Value);

        query = q.SortBy?.ToLowerInvariant() switch
        {
            "highest" => query.OrderByDescending(x => x.OverallScore),
            "latest" => query.OrderByDescending(x => x.CreatedAt),
            _ => query.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.CreatedAt)
        };

        var list = await query.Skip((q.Page - 1) * q.PageSize).Take(q.PageSize).ToListAsync(ct);
        var result = new List<ResultDto>(list.Count);
        foreach (var r in list) result.Add(await Map(r, ct));
        return Result<IReadOnlyCollection<ResultDto>>.Ok(result);
    }

    public async Task<Result<ResultDto>> Handle(ResultByIdQuery q, CancellationToken ct)
    {
        var r = await db.Results.FirstOrDefaultAsync(x => x.Id == q.Id && !x.IsDeleted && x.IsPublished, ct);
        if (r is null) return Result<ResultDto>.Fail("NOT_FOUND", "Result not found.");

        if (q.TrackView)
        {
            r.IncrementViews();
            await db.ResultViews.AddAsync(new ResultView(r.Id, null, null), ct);
            await db.SaveChangesAsync(ct);
        }

        return Result<ResultDto>.Ok(await Map(r, ct));
    }

    public async Task<Result<IReadOnlyCollection<ResultDto>>> Handle(FeaturedResultsQuery q, CancellationToken ct)
    {
        var list = await db.Results.Where(x => !x.IsDeleted && x.IsPublished && x.IsFeatured)
            .OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.CreatedAt).Take(q.Limit).ToListAsync(ct);
        var result = new List<ResultDto>(list.Count);
        foreach (var r in list) result.Add(await Map(r, ct));
        return Result<IReadOnlyCollection<ResultDto>>.Ok(result);
    }

    public async Task<Result<ResultDto>> Handle(CreateResultCommand c, CancellationToken ct)
    {
        var r = new ResultEntry(c.StudentFullName, c.ExamType, c.OverallScore, c.Language);
        r.Update(c.StudentFullName, c.ExamType, c.OverallScore, c.Language, c.Description, c.ImprovementText,
            c.DurationText, c.Notes, c.BadgeIcon, c.DisplayOrder, c.IsFeatured, c.IsPublished);
        await db.Results.AddAsync(r, ct);
        await db.SaveChangesAsync(ct);

        foreach (var item in c.ScoreBreakdown)
        {
            await db.ResultScoreBreakdowns.AddAsync(new ResultScoreBreakdown(r.Id, item.Key, item.Value), ct);
        }

        await db.SaveChangesAsync(ct);
        return Result<ResultDto>.Ok(await Map(r, ct));
    }

    public async Task<Result<ResultDto>> Handle(UpdateResultCommand c, CancellationToken ct)
    {
        var r = await db.Results.FirstOrDefaultAsync(x => x.Id == c.Id && !x.IsDeleted, ct);
        if (r is null) return Result<ResultDto>.Fail("NOT_FOUND", "Result not found.");

        r.Update(c.StudentFullName, c.ExamType, c.OverallScore, c.Language, c.Description, c.ImprovementText,
            c.DurationText, c.Notes, c.BadgeIcon, c.DisplayOrder, c.IsFeatured, c.IsPublished);

        var existing = await db.ResultScoreBreakdowns.Where(x => x.ResultId == r.Id && !x.IsDeleted).ToListAsync(ct);
        foreach (var e in existing) e.SoftDelete();
        foreach (var item in c.ScoreBreakdown)
        {
            await db.ResultScoreBreakdowns.AddAsync(new ResultScoreBreakdown(r.Id, item.Key, item.Value), ct);
        }

        await db.SaveChangesAsync(ct);
        return Result<ResultDto>.Ok(await Map(r, ct));
    }

    public async Task<Result> Handle(DeleteResultCommand c, CancellationToken ct)
    {
        var r = await db.Results.FirstOrDefaultAsync(x => x.Id == c.Id && !x.IsDeleted, ct);
        if (r is null) return Result.Fail("NOT_FOUND", "Result not found.");
        r.SoftDelete();
        await db.SaveChangesAsync(ct);
        return Result.Ok("Result deleted.");
    }

    public async Task<Result<ResultImageDto>> Handle(UploadResultImageCommand c, CancellationToken ct)
    {
        var r = await db.Results.FirstOrDefaultAsync(x => x.Id == c.ResultId && !x.IsDeleted, ct);
        if (r is null) return Result<ResultImageDto>.Fail("NOT_FOUND", "Result not found.");

        var (imageUrl, thumbUrl) = await imageService.SaveAsync(c.FileStream, c.FileName, ct);
        var img = new ResultImage(r.Id, imageUrl, thumbUrl, c.IsMain);
        await db.ResultImages.AddAsync(img, ct);
        if (c.IsMain) r.SetMainImage(imageUrl);
        await db.SaveChangesAsync(ct);
        return Result<ResultImageDto>.Ok(new ResultImageDto(img.Id, img.ImageUrl, img.ThumbnailUrl, img.IsMain));
    }

    public async Task<Result<ResultImageDto>> Handle(ReplaceResultImageCommand c, CancellationToken ct)
    {
        var img = await db.ResultImages.FirstOrDefaultAsync(x => x.Id == c.ImageId && x.ResultId == c.ResultId && !x.IsDeleted, ct);
        if (img is null) return Result<ResultImageDto>.Fail("NOT_FOUND", "Image not found.");

        await imageService.DeleteAsync(img.ImageUrl, img.ThumbnailUrl, ct);
        var (imageUrl, thumbUrl) = await imageService.SaveAsync(c.FileStream, c.FileName, ct);
        img.Replace(imageUrl, thumbUrl);

        var result = await db.Results.FirstOrDefaultAsync(x => x.Id == c.ResultId, ct);
        if (result is not null && img.IsMain) result.SetMainImage(imageUrl);

        await db.SaveChangesAsync(ct);
        return Result<ResultImageDto>.Ok(new ResultImageDto(img.Id, img.ImageUrl, img.ThumbnailUrl, img.IsMain));
    }

    public async Task<Result> Handle(DeleteResultImageCommand c, CancellationToken ct)
    {
        var img = await db.ResultImages.FirstOrDefaultAsync(x => x.Id == c.ImageId && x.ResultId == c.ResultId && !x.IsDeleted, ct);
        if (img is null) return Result.Fail("NOT_FOUND", "Image not found.");
        img.SoftDelete();
        await imageService.DeleteAsync(img.ImageUrl, img.ThumbnailUrl, ct);
        await db.SaveChangesAsync(ct);
        return Result.Ok("Image deleted.");
    }

    public async Task<Result<ResultsAdminStatsDto>> Handle(ResultsAdminStatsQuery q, CancellationToken ct)
    {
        var total = await db.Results.CountAsync(x => !x.IsDeleted, ct);
        var ielts = await db.Results.CountAsync(x => !x.IsDeleted && x.ExamType == ExamType.IELTS, ct);
        var sat = await db.Results.CountAsync(x => !x.IsDeleted && x.ExamType == ExamType.SAT, ct);
        var featured = await db.Results.CountAsync(x => !x.IsDeleted && x.IsFeatured, ct);
        var mostViewed = await db.Results.Where(x => !x.IsDeleted).OrderByDescending(x => x.ViewsCount)
            .Select(x => (Guid?)x.Id).FirstOrDefaultAsync(ct);
        var recent = await db.Results.Where(x => !x.IsDeleted).OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync(ct);
        var recentDtos = new List<ResultDto>();
        foreach (var r in recent) recentDtos.Add(await Map(r, ct));

        return Result<ResultsAdminStatsDto>.Ok(new ResultsAdminStatsDto(total, ielts, sat, featured, mostViewed, recentDtos));
    }
}

