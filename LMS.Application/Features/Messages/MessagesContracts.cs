using FluentValidation;
using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Messages;

public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    string Text,
    DateTime CreatedAt,
    DateTime? ReadAt);

public sealed record MessagesPingCommand : IRequest<Result<string>>;

public sealed class MessagesPingCommandHandler : IRequestHandler<MessagesPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(MessagesPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Messages module ready"));
    }
}

/// <summary>
/// Sends a message to a conversation. <c>SenderUserId</c> is IGNORED — the
/// handler always uses the caller's user id from the JWT. The field is kept
/// in the contract only for backwards compatibility with existing clients.
/// </summary>
public sealed record SendMessageCommand(
    Guid ConversationId,
    Guid SenderUserId,
    string Text) : IRequest<Result<MessageDto>>;

public sealed class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.ConversationId).NotEmpty();
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Message text is required.")
            .MaximumLength(4000).WithMessage("Message text must be 4000 characters or fewer.");
    }
}

/// <summary>
/// Loads a page of messages from a conversation, newest-first when paged via
/// <paramref name="Before"/>. Returns at most <paramref name="Limit"/> rows
/// (capped server-side to 200). The caller must be a participant.
/// </summary>
public sealed record GetConversationMessagesQuery(
    Guid ConversationId,
    DateTime? Before = null,
    int Limit = 50)
    : IRequest<Result<IReadOnlyCollection<MessageDto>>>;

public sealed record MarkMessageAsReadCommand(Guid MessageId) : IRequest<Result<MessageDto>>;

/// <summary>
/// Count of unread messages addressed to the given user across all conversations they participate in.
/// </summary>
public sealed record GetUnreadMessageCountQuery(Guid UserId) : IRequest<Result<int>>;
