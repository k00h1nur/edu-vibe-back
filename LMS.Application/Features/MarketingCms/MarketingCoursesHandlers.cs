using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.MarketingCms;

public sealed class MarketingCoursesHandlers(IApplicationDbContext db) :
    IRequestHandler<GetMarketingCoursesQuery, Result<IReadOnlyCollection<MarketingCourseDto>>>,
    IRequestHandler<GetPublicMarketingCoursesQuery, Result<IReadOnlyCollection<MarketingCourseDto>>>,
    IRequestHandler<CreateMarketingCourseCommand, Result<MarketingCourseDto>>,
    IRequestHandler<UpdateMarketingCourseCommand, Result<MarketingCourseDto>>,
    IRequestHandler<DeleteMarketingCourseCommand, Result>
{
    public async Task<Result<IReadOnlyCollection<MarketingCourseDto>>> Handle(
        GetMarketingCoursesQuery request, CancellationToken ct)
    {
        var q = db.MarketingCourses.AsNoTracking();
        if (request.OnlyActive) q = q.Where(c => c.IsActive);
        var items = await q
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .Select(Project()).ToListAsync(ct);
        return Result<IReadOnlyCollection<MarketingCourseDto>>.Ok(items);
    }

    public async Task<Result<IReadOnlyCollection<MarketingCourseDto>>> Handle(
        GetPublicMarketingCoursesQuery request, CancellationToken ct)
    {
        var items = await db.MarketingCourses.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Title)
            .Select(Project()).ToListAsync(ct);
        return Result<IReadOnlyCollection<MarketingCourseDto>>.Ok(items);
    }

    public async Task<Result<MarketingCourseDto>> Handle(CreateMarketingCourseCommand request, CancellationToken ct)
    {
        if (await db.MarketingCourses.AnyAsync(c => c.Slug == request.Slug.ToLower(), ct))
            return Result<MarketingCourseDto>.Fail("VALIDATION", "A course with this slug already exists.");

        var c = new MarketingCourse(
            request.Slug, request.Title, request.Subtitle, request.Description,
            request.CoverImageUrl, request.PriceText, request.DurationText, request.LevelText,
            request.SortOrder, request.IsActive);
        await db.MarketingCourses.AddAsync(c, ct);
        await db.SaveChangesAsync(ct);
        return Result<MarketingCourseDto>.Ok(Map(c));
    }

    public async Task<Result<MarketingCourseDto>> Handle(UpdateMarketingCourseCommand request, CancellationToken ct)
    {
        var c = await db.MarketingCourses.FirstOrDefaultAsync(x => x.Id == request.CourseId, ct);
        if (c is null) return Result<MarketingCourseDto>.Fail("NOT_FOUND", "Course not found.");
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await db.MarketingCourses.AnyAsync(x => x.Slug == slug && x.Id != request.CourseId, ct))
            return Result<MarketingCourseDto>.Fail("VALIDATION", "A course with this slug already exists.");

        c.Update(request.Slug, request.Title, request.Subtitle, request.Description,
            request.CoverImageUrl, request.PriceText, request.DurationText, request.LevelText,
            request.SortOrder, request.IsActive);
        await db.SaveChangesAsync(ct);
        return Result<MarketingCourseDto>.Ok(Map(c));
    }

    public async Task<Result> Handle(DeleteMarketingCourseCommand request, CancellationToken ct)
    {
        var c = await db.MarketingCourses.FirstOrDefaultAsync(x => x.Id == request.CourseId, ct);
        if (c is null) return Result.Fail("NOT_FOUND", "Course not found.");
        db.MarketingCourses.Remove(c);
        await db.SaveChangesAsync(ct);
        return Result.Ok("Deleted");
    }

    private static System.Linq.Expressions.Expression<Func<MarketingCourse, MarketingCourseDto>> Project()
        => c => new MarketingCourseDto(
            c.Id, c.Slug, c.Title, c.Subtitle, c.Description, c.CoverImageUrl,
            c.PriceText, c.DurationText, c.LevelText, c.SortOrder, c.IsActive);

    private static MarketingCourseDto Map(MarketingCourse c) => new(
        c.Id, c.Slug, c.Title, c.Subtitle, c.Description, c.CoverImageUrl,
        c.PriceText, c.DurationText, c.LevelText, c.SortOrder, c.IsActive);
}
