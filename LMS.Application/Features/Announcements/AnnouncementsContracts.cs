using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Announcements;

public sealed record AnnouncementDto(
    Guid Id,
    string Title,
    string Body,
    bool IsPublic,
    AnnouncementAudience Audience,
    DateTime? PublishesAt,
    DateTime? ExpiresAt,
    Guid AuthorUserId,
    DateTime CreatedAt);

/// <summary>
/// Signed-in feed of every announcement. Pass <c>onlyLive</c> to hide
/// expired / not-yet-published rows (the dashboard widgets use this).
/// When <c>audienceFilter</c> is set, only announcements whose Audience
/// matches OR Audience=Everyone are returned — student dashboard passes
/// Students, teacher dashboard passes Teachers.
/// </summary>
public sealed record GetAnnouncementsQuery(
    bool OnlyLive = false,
    AnnouncementAudience? AudienceFilter = null)
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
    AnnouncementAudience Audience,
    DateTime? PublishesAt,
    DateTime? ExpiresAt,
    Guid AuthorUserId) : IRequest<Result<AnnouncementDto>>;

public sealed record UpdateAnnouncementCommand(
    Guid AnnouncementId,
    string Title,
    string Body,
    bool IsPublic,
    AnnouncementAudience Audience,
    DateTime? PublishesAt,
    DateTime? ExpiresAt) : IRequest<Result<AnnouncementDto>>;

public sealed record DeleteAnnouncementCommand(Guid AnnouncementId) : IRequest<Result>;
