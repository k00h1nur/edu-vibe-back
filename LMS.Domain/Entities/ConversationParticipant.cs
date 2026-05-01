using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class ConversationParticipant : BaseEntity
{
    public ConversationParticipant(Guid conversationId, Guid userId)
    {
        if (conversationId == Guid.Empty || userId == Guid.Empty) throw new DomainException("Ids are required.");
        ConversationId = conversationId;
        UserId = userId;
    }

    public Guid ConversationId { get; private set; }
    public Guid UserId { get; private set; }
}