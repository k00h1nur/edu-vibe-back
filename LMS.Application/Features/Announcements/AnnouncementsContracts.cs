using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Announcements;

public sealed record AnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    bool IsPublic,
    DateTime? PublishesAt,
    DateTime? ExpiresAt,
    Guid AuthorUserId,
    DateTime CreatedAt);

/// <summary>
/// Admin / signed-in feed of every announcement. Pass <c>onlyLive</c> to
/// hide expired / not-yet-published rows (the student widget uses this).
/// </summary>
public sealed record GetAnnouncementsQuery(bool OnlyLive = false)
    : IRequest<Result<IReadOnlyCollection<AnnouncementDto>>>;

/// <summary>
/// Public marketing feed — only IsPublic + currently inside their visibility
/// window. Returned ordered newest-first. No auth required.
/// </summary>
public sealed record GetPublicAnnouncementsQuery(int Take = 10)
    : IRequest<Result<IReadOnlyCollection<AnnouncementDto>>>;

public sealed record CreateAnnouncementCommand(
    string Title,
    string Body,
    bool IsPublic,
    DateTime? PublishesAt,
    DateTime? ExpiresAt,
    Guid AuthorUserId) : IRequest<Result<AnnouncementDto>>;

public sealed record UpdateAnnouncementCommand(
    Guid AnnouncementId,
    string Title,
    string Body,
    bool IsPublic,
    DateTime? PublishesAt,
    DateTime? ExpiresAt) : IRequest<Result<AnnouncementDto>>;

public sealed record DeleteAnnouncementCommand(Guid AnnouncementId) : IRequest<Result>;
