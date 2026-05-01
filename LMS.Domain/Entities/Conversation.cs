using LMS.Domain.Common;
using LMS.Domain.Enums;

namespace LMS.Domain.Entities;

public sealed class Conversation : BaseEntity
{
    public Conversation(ConversationType type, string? title = null)
    {
        Type = type;
        Title = title;
    }

    public ConversationType Type { get; private set; }
    public string? Title { get; private set; }
}