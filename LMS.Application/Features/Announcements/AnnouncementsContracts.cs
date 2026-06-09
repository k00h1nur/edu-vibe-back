using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Announcements;

public sealed record AnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    bool IsPublished,
    DateTime? PublishedAt,
    Guid AuthorUserId,
    DateTime CreatedAt);

public sealed record GetAnnouncementsQuery(bool PublishedOnly)
    : IRequest<Result<IReadOnlyCollection<AnnouncementDto>>>;

public sealed record CreateAnnouncementCommand(string Title, string Body, Guid AuthorUserId)
    : IRequest<Result<AnnouncementDto>>;

public sealed record UpdateAnnouncementCommand(Guid AnnouncementId, string Title, string Body)
    : IRequest<Result<AnnouncementDto>>;

public sealed record SetAnnouncementPublishedCommand(Guid AnnouncementId, bool IsPublished)
    : IRequest<Result<AnnouncementDto>>;

public sealed record DeleteAnnouncementCommand(Guid AnnouncementId) : IRequest<Result>;
