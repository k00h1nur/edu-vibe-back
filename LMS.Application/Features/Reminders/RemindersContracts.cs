using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Reminders;

public sealed record ReminderDto(
    Guid Id,
    string Title,
    string? Notes,
    DateTime DueAt,
    bool IsCompleted,
    DateTime? CompletedAt,
    DateTime CreatedAt);

public sealed record GetMyRemindersQuery(Guid OwnerUserId, bool IncludeCompleted = true)
    : IRequest<Result<IReadOnlyCollection<ReminderDto>>>;

public sealed record CreateReminderCommand(Guid OwnerUserId, string Title, string? Notes, DateTime DueAt)
    : IRequest<Result<ReminderDto>>;

public sealed record UpdateReminderCommand(Guid ReminderId, Guid OwnerUserId, string Title, string? Notes, DateTime DueAt)
    : IRequest<Result<ReminderDto>>;

public sealed record SetReminderCompletedCommand(Guid ReminderId, Guid OwnerUserId, bool IsCompleted)
    : IRequest<Result<ReminderDto>>;

public sealed record DeleteReminderCommand(Guid ReminderId, Guid OwnerUserId) : IRequest<Result>;
