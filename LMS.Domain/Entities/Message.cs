using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class Message : BaseEntity
{
    public Message(Guid conversationId, Guid senderUserId, string text)
    {
        if (conversationId == Guid.Empty || senderUserId == Guid.Empty) throw new DomainException("Ids are required.");
        if (string.IsNullOrWhiteSpace(text)) throw new DomainException("Message text is required.");
        ConversationId = conversationId;
        SenderUserId = senderUserId;
        Text = text.Trim();
    }

    public Guid ConversationId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public string Text { get; private set; }
    public DateTime? ReadAt { get; private set; }

    public void MarkAsRead()
    {
        ReadAt = DateTime.UtcNow;
    }
}