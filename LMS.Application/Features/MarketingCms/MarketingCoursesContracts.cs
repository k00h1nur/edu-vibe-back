using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.MarketingCms;

public sealed record MarketingCourseDto(
    Guid Id,
    string Slug,
    string Title,
    string? Subtitle,
    string? Description,
    string? CoverImageUrl,
    string? PriceText,
    string? DurationText,
    string? LevelText,
    int SortOrder,
    bool IsActive);

/// <summary>Admin / signed-in listing. Pass <c>onlyActive</c> for the marketing-side preview.</summary>
public sealed record GetMarketingCoursesQuery(bool OnlyActive = false)
    : IRequest<Result<IReadOnlyCollection<MarketingCourseDto>>>;

public sealed record GetPublicMarketingCoursesQuery
    : IRequest<Result<IReadOnlyCollection<MarketingCourseDto>>>;

public sealed record CreateMarketingCourseCommand(
    string Slug, string Title, string? Subtitle, string? Description,
    string? CoverImageUrl, string? PriceText, string? DurationText, string? LevelText,
    int SortOrder, bool IsActive) : IRequest<Result<MarketingCourseDto>>;

public sealed record UpdateMarketingCourseCommand(
    Guid CourseId,
    string Slug, string Title, string? Subtitle, string? Description,
    string? CoverImageUrl, string? PriceText, string? DurationText, string? LevelText,
    int SortOrder, bool IsActive) : IRequest<Result<MarketingCourseDto>>;

public sealed record DeleteMarketingCourseCommand(Guid CourseId) : IRequest<Result>;
