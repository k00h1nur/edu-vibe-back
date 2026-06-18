using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Messages;

public sealed class MessagesHandlers(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications) :
    IRequestHandler<SendMessageCommand, Result<MessageDto>>,
    IRequestHandler<GetConversationMessagesQuery, Result<IReadOnlyCollection<MessageDto>>>,
    IRequestHandler<MarkMessageAsReadCommand, Result<MessageDto>>,
    IRequestHandler<GetUnreadMessageCountQuery, Result<int>>
{
    /// <summary>Hard cap on the page size, no matter what the caller asks for.</summary>
    private const int MaxPageSize = 200;

    public async Task<Result<IReadOnlyCollection<MessageDto>>> Handle(
        GetConversationMessagesQuery request, CancellationToken cancellationToken)
    {
        // SECURITY: only participants can read a conversation's messages.
        // Without this gate, anyone holding Messages.Read could enumerate
        // any conversation by id.
        var callerId = currentUser.UserId;
        if (callerId is null)
            return Result<IReadOnlyCollection<MessageDto>>.Fail("FORBIDDEN", "Sign in required.");

        var isParticipant = await db.ConversationParticipants.AnyAsync(
            p => p.ConversationId == request.ConversationId && p.UserId == callerId,
            cancellationToken);
        if (!isParticipant)
            return Result<IReadOnlyCollection<MessageDto>>.Fail(
                "FORBIDDEN", "Not a participant of this conversation.");

        // Cursor pagination: newest-first, capped page size, optional Before cursor.
        var limit = Math.Clamp(request.Limit, 1, MaxPageSize);
        var query = db.Messages
            .AsNoTracking()
            .Where(m => m.ConversationId == request.ConversationId);
        if (request.Before is { } before)
            query = query.Where(m => m.CreatedAt < before);

        var page = await query
            .OrderByDescending(m => m.CreatedAt)
            .Take(limit)
            .Select(m => new MessageDto(
                m.Id, m.ConversationId, m.SenderUserId, m.Text, m.CreatedAt, m.ReadAt))
            .ToListAsync(cancellationToken);

        // Return chronological order so a chat UI can render the page top-down
        // without reversing client-side. The cursor (Before = oldest.CreatedAt)
        // still steps backwards through history one page at a time.
        page.Reverse();
        return Result<IReadOnlyCollection<MessageDto>>.Ok(page);
    }

    public async Task<Result<MessageDto>> Handle(
        MarkMessageAsReadCommand request, CancellationToken cancellationToken)
    {
        var callerId = currentUser.UserId;
        if (callerId is null) return Result<MessageDto>.Fail("FORBIDDEN", "Sign in required.");

        var m = await db.Messages.FirstOrDefaultAsync(
            x => x.Id == request.MessageId, cancellationToken);
        if (m is null) return Result<MessageDto>.Fail("NOT_FOUND", "Message not found.");

        // A user can only mark messages addressed to THEM as read:
        //   1. They must be a participant of the conversation, AND
        //   2. They must not be the original sender (sender can't mark their own
        //      messages as read — there's nothing to read on their side).
        if (m.SenderUserId == callerId)
            return Result<MessageDto>.Fail("FORBIDDEN", "Cannot mark your own message as read.");

        var isParticipant = await db.ConversationParticipants.AnyAsync(
            p => p.ConversationId == m.ConversationId && p.UserId == callerId,
            cancellationToken);
        if (!isParticipant)
            return Result<MessageDto>.Fail("FORBIDDEN", "Not a participant of this conversation.");

        m.MarkAsRead();
        await db.SaveChangesAsync(cancellationToken);
        return Result<MessageDto>.Ok(Map(m));
    }

    public async Task<Result<MessageDto>> Handle(
        SendMessageCommand request, CancellationToken cancellationToken)
    {
        // SECURITY: ignore the SenderUserId on the wire. The handler always
        // uses the authenticated caller. Without this a user could spoof
        // another participant just by passing a different id in the body.
        var callerId = currentUser.UserId;
        if (callerId is null) return Result<MessageDto>.Fail("FORBIDDEN", "Sign in required.");

        var isPart = await db.ConversationParticipants.AnyAsync(
            x => x.ConversationId == request.ConversationId && x.UserId == callerId,
            cancellationToken);
        if (!isPart) return Result<MessageDto>.Fail("FORBIDDEN", "Sender is not a participant.");

        var m = new Message(request.ConversationId, callerId.Value, request.Text);
        await db.Messages.AddAsync(m, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        // Telegram DM the other participant(s) — works for any role (student,
        // teacher, staff), since everyone signs in through the platform bot.
        var recipientIds = await db.ConversationParticipants
            .Where(p => p.ConversationId == request.ConversationId && p.UserId != callerId.Value)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);
        var preview = request.Text.Length > 140 ? request.Text[..140] + "…" : request.Text;
        await notifications.NotifyUsersAsync(
            recipientIds, $"💬 New message on EduVibe:\n{preview}", cancellationToken);

        return Result<MessageDto>.Ok(Map(m));
    }

    public async Task<Result<int>> Handle(
        GetUnreadMessageCountQuery request, CancellationToken cancellationToken)
    {
        // Single query: subquery via Any() instead of materializing every
        // conversation id first. With ix_conversation_participants_user_id +
        // ix_messages_conversation_read_at this is two index seeks total.
        var count = await db.Messages
            .Where(m => m.SenderUserId != request.UserId
                        && m.ReadAt == null
                        && db.ConversationParticipants.Any(
                            p => p.ConversationId == m.ConversationId && p.UserId == request.UserId))
            .CountAsync(cancellationToken);
        return Result<int>.Ok(count);
    }

    private static MessageDto Map(Message m) =>
        new(m.Id, m.ConversationId, m.SenderUserId, m.Text, m.CreatedAt, m.ReadAt);
}
