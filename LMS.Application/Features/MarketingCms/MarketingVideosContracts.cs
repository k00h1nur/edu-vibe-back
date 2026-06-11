using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.MarketingCms;

public sealed record MarketingVideoDto(
    Guid Id,
    string Title,
    string? Description,
    string VideoUrl,
    string? ThumbnailUrl,
    int SortOrder,
    bool IsActive);

public sealed record GetMarketingVideosQuery(bool OnlyActive = false)
    : IRequest<Result<IReadOnlyCollection<MarketingVideoDto>>>;

public sealed record GetPublicMarketingVideosQuery
    : IRequest<Result<IReadOnlyCollection<MarketingVideoDto>>>;

public sealed record CreateMarketingVideoCommand(
    string Title, string? Description, string VideoUrl, string? ThumbnailUrl,
    int SortOrder, bool IsActive) : IRequest<Result<MarketingVideoDto>>;

public sealed record UpdateMarketingVideoCommand(
    Guid VideoId, string Title, string? Description, string VideoUrl, string? ThumbnailUrl,
    int SortOrder, bool IsActive) : IRequest<Result<MarketingVideoDto>>;

public sealed record DeleteMarketingVideoCommand(Guid VideoId) : IRequest<Result>;
