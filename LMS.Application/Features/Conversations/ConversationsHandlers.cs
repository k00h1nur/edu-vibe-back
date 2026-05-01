using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Conversations;

public sealed class ConversationsHandlers(IApplicationDbContext db) :
    IRequestHandler<CreateConversationCommand, Result<ConversationDto>>,
    IRequestHandler<AddConversationParticipantCommand, Result>,
    IRequestHandler<RemoveConversationParticipantCommand, Result>,
    IRequestHandler<GetMyConversationsQuery, Result<IReadOnlyCollection<ConversationDto>>>
{
    public async Task<Result> Handle(AddConversationParticipantCommand request, CancellationToken cancellationToken)
    {
        if (await db.ConversationParticipants.AnyAsync(
                x => x.ConversationId == request.ConversationId && x.UserId == request.UserId, cancellationToken))
            return Result.Ok("Already participant");
        await db.ConversationParticipants.AddAsync(new ConversationParticipant(request.ConversationId, request.UserId),
            cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Added");
    }

    public async Task<Result<ConversationDto>> Handle(CreateConversationCommand request,
        CancellationToken cancellationToken)
    {
        var c = new Conversation(request.Type, request.Title);
        await db.Conversations.AddAsync(c, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        foreach (var uid in request.ParticipantUserIds.Distinct())
            await db.ConversationParticipants.AddAsync(new ConversationParticipant(c.Id, uid), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<ConversationDto>.Ok(new ConversationDto(c.Id, c.Type, c.Title));
    }

    public async Task<Result<IReadOnlyCollection<ConversationDto>>> Handle(GetMyConversationsQuery request,
        CancellationToken cancellationToken)
    {
        var ids = await db.ConversationParticipants.Where(x => x.UserId == request.UserId).Select(x => x.ConversationId)
            .ToListAsync(cancellationToken);
        var list = await db.Conversations.Where(x => ids.Contains(x.Id))
            .Select(c => new ConversationDto(c.Id, c.Type, c.Title)).ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<ConversationDto>>.Ok(list);
    }

    public async Task<Result> Handle(RemoveConversationParticipantCommand request, CancellationToken cancellationToken)
    {
        var p = await db.ConversationParticipants.FirstOrDefaultAsync(
            x => x.ConversationId == request.ConversationId && x.UserId == request.UserId, cancellationToken);
        if (p is null) return Result.Fail("NOT_FOUND", "Participant not found.");
        db.ConversationParticipants.Remove(p);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Removed");
    }
}