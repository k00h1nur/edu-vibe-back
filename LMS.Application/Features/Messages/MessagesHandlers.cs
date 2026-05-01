using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Messages;

public sealed class MessagesHandlers(IApplicationDbContext db) :
    IRequestHandler<SendMessageCommand, Result<MessageDto>>,
    IRequestHandler<GetConversationMessagesQuery, Result<IReadOnlyCollection<MessageDto>>>,
    IRequestHandler<MarkMessageAsReadCommand, Result<MessageDto>>
{
    public async Task<Result<IReadOnlyCollection<MessageDto>>> Handle(GetConversationMessagesQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<MessageDto>>.Ok(await db.Messages
            .Where(x => x.ConversationId == request.ConversationId)
            .Select(m => new MessageDto(m.Id, m.ConversationId, m.SenderUserId, m.Text, m.ReadAt))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<MessageDto>> Handle(MarkMessageAsReadCommand request, CancellationToken cancellationToken)
    {
        var m = await db.Messages.FirstOrDefaultAsync(x => x.Id == request.MessageId, cancellationToken);
        if (m is null) return Result<MessageDto>.Fail("NOT_FOUND", "Message not found.");
        m.MarkAsRead();
        await db.SaveChangesAsync(cancellationToken);
        return Result<MessageDto>.Ok(Map(m));
    }

    public async Task<Result<MessageDto>> Handle(SendMessageCommand request, CancellationToken cancellationToken)
    {
        var isPart = await db.ConversationParticipants.AnyAsync(
            x => x.ConversationId == request.ConversationId && x.UserId == request.SenderUserId, cancellationToken);
        if (!isPart) return Result<MessageDto>.Fail("FORBIDDEN", "Sender is not participant.");
        var m = new Message(request.ConversationId, request.SenderUserId, request.Text);
        await db.Messages.AddAsync(m, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<MessageDto>.Ok(Map(m));
    }

    private static MessageDto Map(Message m)
    {
        return new MessageDto(m.Id, m.ConversationId, m.SenderUserId, m.Text, m.ReadAt);
    }
}