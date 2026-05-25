using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;

namespace LMS.Application.Features.VisitorMessages;

public sealed record VisitorMessageDto(
    Guid Id,
    string Name,
    string Phone,
    string? Email,
    string Message,
    VisitorMessageSource Source,
    string? Course,
    string? PreferredTime,
    string? Language,
    bool IsRead,
    DateTime? ReadAt,
    DateTime CreatedAt);

/// <summary>
/// Inbound message from a visitor. Anonymous endpoint — used by the marketing
/// site's contact form and demo-lesson form.
/// </summary>
public sealed record CreateVisitorMessageCommand(
    string Name,
    string Phone,
    string? Email,
    string Message,
    VisitorMessageSource Source,
    string? Course = null,
    string? PreferredTime = null,
    string? Language = null) : IRequest<Result<VisitorMessageDto>>;

/// <summary>Admin inbox query — paginated, filterable by read state.</summary>
public sealed record GetVisitorMessagesQuery(
    bool? IsRead = null,
    VisitorMessageSource? Source = null,
    int Page = 1,
    int PageSize = 25)
    : IRequest<Result<VisitorMessagePage>>;

public sealed record VisitorMessagePage(
    IReadOnlyCollection<VisitorMessageDto> Items,
    int Total,
    int Page,
    int PageSize,
    int UnreadCount);

public sealed record MarkVisitorMessageReadCommand(Guid Id, bool Read = true)
    : IRequest<Result<VisitorMessageDto>>;

public sealed record GetUnreadVisitorMessageCountQuery : IRequest<Result<int>>;
