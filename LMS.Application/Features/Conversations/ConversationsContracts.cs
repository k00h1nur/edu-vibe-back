using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Conversations;

public sealed record ConversationDto(Guid Id, ConversationType Type, string? Title);

public sealed record ConversationsPingCommand : IRequest<Result<string>>;

public sealed class ConversationsPingCommandHandler : IRequestHandler<ConversationsPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(ConversationsPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Conversations module ready"));
    }
}

public sealed record CreateConversationCommand(
    ConversationType Type,
    string? Title,
    IReadOnlyCollection<Guid> ParticipantUserIds) : IRequest<Result<ConversationDto>>;

public sealed record AddConversationParticipantCommand(Guid ConversationId, Guid UserId) : IRequest<Result>;

public sealed record RemoveConversationParticipantCommand(Guid ConversationId, Guid UserId) : IRequest<Result>;

public sealed record GetMyConversationsQuery(Guid UserId) : IRequest<Result<IReadOnlyCollection<ConversationDto>>>;