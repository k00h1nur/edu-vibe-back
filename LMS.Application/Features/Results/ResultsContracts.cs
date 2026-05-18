using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Results;

public sealed record ScoreBreakdownItemDto(string Key, string Value);
public sealed record ResultImageDto(Guid Id, string ImageUrl, string? ThumbnailUrl, bool IsMain);

public sealed record ResultDto(
    Guid Id,
    string StudentFullName,
    string? MainImageUrl,
    ExamType ExamType,
    decimal OverallScore,
    string? Description,
    string? ImprovementText,
    string? DurationText,
    string? Notes,
    string? BadgeIcon,
    int DisplayOrder,
    bool IsFeatured,
    bool IsPublished,
    string Language,
    DateTime CreatedAt,
    int ViewsCount,
    IReadOnlyCollection<ScoreBreakdownItemDto> ScoreBreakdown,
    IReadOnlyCollection<ResultImageDto> Images);

public sealed record ResultListQuery(
    string? Search,
    ExamType? ExamType,
    bool? Featured,
    string? SortBy,
    int Page = 1,
    int PageSize = 12) : IRequest<Result<IReadOnlyCollection<ResultDto>>>;

public sealed record ResultByIdQuery(Guid Id, bool TrackView = true) : IRequest<Result<ResultDto>>;
public sealed record FeaturedResultsQuery(int Limit = 6) : IRequest<Result<IReadOnlyCollection<ResultDto>>>;

public sealed record CreateResultCommand(
    string StudentFullName,
    ExamType ExamType,
    decimal OverallScore,
    string? Description,
    string? ImprovementText,
    string? DurationText,
    string? Notes,
    string? BadgeIcon,
    int DisplayOrder,
    bool IsFeatured,
    bool IsPublished,
    string Language,
    IReadOnlyCollection<ScoreBreakdownItemDto> ScoreBreakdown) : IRequest<Result<ResultDto>>;

public sealed record UpdateResultCommand(
    Guid Id,
    string StudentFullName,
    ExamType ExamType,
    decimal OverallScore,
    string? Description,
    string? ImprovementText,
    string? DurationText,
    string? Notes,
    string? BadgeIcon,
    int DisplayOrder,
    bool IsFeatured,
    bool IsPublished,
    string Language,
    IReadOnlyCollection<ScoreBreakdownItemDto> ScoreBreakdown) : IRequest<Result<ResultDto>>;

public sealed record DeleteResultCommand(Guid Id) : IRequest<Result>;
public sealed record UploadResultImageCommand(Guid ResultId, Stream FileStream, string FileName, bool IsMain) : IRequest<Result<ResultImageDto>>;
public sealed record ReplaceResultImageCommand(Guid ResultId, Guid ImageId, Stream FileStream, string FileName) : IRequest<Result<ResultImageDto>>;
public sealed record DeleteResultImageCommand(Guid ResultId, Guid ImageId) : IRequest<Result>;

public sealed record ResultsAdminStatsDto(
    int TotalResults,
    int IeltsCount,
    int SatCount,
    int FeaturedCount,
    Guid? MostViewedResultId,
    IReadOnlyCollection<ResultDto> RecentAdded);

public sealed record ResultsAdminStatsQuery() : IRequest<Result<ResultsAdminStatsDto>>;
