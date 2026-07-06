using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Conversations;

public sealed record ConversationDto(Guid Id, ConversationType Type, string? Title);

public sealed record CreateConversationCommand(
    ConversationType Type,
    string? Title,
    IReadOnlyCollection<Guid> ParticipantUserIds) : IRequest<Result<ConversationDto>>;

public sealed record AddConversationParticipantCommand(Guid ConversationId, Guid UserId) : IRequest<Result>;

public sealed record RemoveConversationParticipantCommand(Guid ConversationId, Guid UserId) : IRequest<Result>;

public sealed record GetMyConversationsQuery(Guid UserId) : IRequest<Result<IReadOnlyCollection<ConversationDto>>>;

/// <summary>A user the caller is allowed to start a conversation with.</summary>
public sealed record ContactDto(Guid UserId, string Name, string Role);

/// <summary>
/// The people the current user may message, scoped by role: a student sees their
/// classmates + the teachers of their classes + admins; a teacher sees the
/// students in classes they teach + admins; an admin sees everyone.
/// </summary>
public sealed record GetMessageableContactsQuery : IRequest<Result<IReadOnlyCollection<ContactDto>>>;