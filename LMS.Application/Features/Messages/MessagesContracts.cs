using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Messages;

public sealed record MessageDto(Guid Id, Guid ConversationId, Guid SenderUserId, string Text, DateTime? ReadAt);

public sealed record MessagesPingCommand : IRequest<Result<string>>;

public sealed class MessagesPingCommandHandler : IRequestHandler<MessagesPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(MessagesPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Messages module ready"));
    }
}

public sealed record SendMessageCommand(Guid ConversationId, Guid SenderUserId, string Text)
    : IRequest<Result<MessageDto>>;

public sealed record GetConversationMessagesQuery(Guid ConversationId)
    : IRequest<Result<IReadOnlyCollection<MessageDto>>>;

public sealed record MarkMessageAsReadCommand(Guid MessageId) : IRequest<Result<MessageDto>>;