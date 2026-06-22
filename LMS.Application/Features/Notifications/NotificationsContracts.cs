using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Notifications;

/// <summary>
/// One row in the in-app notification feed. Deliberately presentation-light:
/// <see cref="Type"/> drives the icon + the deep-link the client builds for its
/// own panel (the backend doesn't know panel routes), <see cref="Title"/> is the
/// sender name or inquiry label, <see cref="Body"/> a short preview.
/// </summary>
public sealed record NotificationDto(
    string Id,        // "message:{guid}" | "inquiry:{guid}" — stable React key
    string Type,      // "message" | "inquiry"
    string Title,
    string Body,
    DateTime CreatedAt);

/// <summary>
/// The caller's recent, actionable notifications, newest first: unread direct
/// messages (with the sender's name + preview) and — for staff — the latest
/// unhandled visitor inquiries (demo / mock test / level check / contact).
/// Self-scoped: always the authenticated caller, never a wire id.
/// </summary>
public sealed record GetNotificationsQuery(int Take = 15)
    : IRequest<Result<IReadOnlyCollection<NotificationDto>>>;
