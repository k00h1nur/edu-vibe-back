using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Conversations;

public sealed class ConversationsHandlers(
    IApplicationDbContext db,
    ICurrentUserService currentUser) :
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
        await db.ConversationParticipants.AddAsync(
            new ConversationParticipant(request.ConversationId, request.UserId), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Added");
    }

    public async Task<Result<ConversationDto>> Handle(
        CreateConversationCommand request, CancellationToken cancellationToken)
    {
        // Auto-include the creator as a participant so they can immediately
        // post into and read from the conversation they just opened. The
        // previous behaviour required a separate "add me as participant" call.
        var participants = request.ParticipantUserIds.ToHashSet();
        if (currentUser.UserId is { } me) participants.Add(me);
        if (participants.Count == 0)
            return Result<ConversationDto>.Fail("VALIDATION", "At least one participant is required.");

        var c = new Conversation(request.Type, request.Title);
        await db.Conversations.AddAsync(c, cancellationToken);

        // EF assigns the PK on first SaveChanges. Stage participants now and
        // commit them in the same batch so a crash mid-flight can't leave a
        // headless conversation lying around with no members.
        await db.SaveChangesAsync(cancellationToken);

        var participantRows = participants
            .Select(uid => new ConversationParticipant(c.Id, uid))
            .ToList();
        await db.ConversationParticipants.AddRangeAsync(participantRows, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        return Result<ConversationDto>.Ok(new ConversationDto(c.Id, c.Type, c.Title));
    }

    public async Task<Result<IReadOnlyCollection<ConversationDto>>> Handle(
        GetMyConversationsQuery request, CancellationToken cancellationToken)
    {
        // Single SQL join (participant → conversation) instead of the previous
        // two-query waterfall (participant ids → Contains() lookup), so a long
        // participant list doesn't blow up the IN clause and the round-trip
        // count halves. Explicit join because ConversationParticipant has no
        // navigation property (sticking with foreign-key-by-id, no inverse nav).
        var list = await (
            from p in db.ConversationParticipants.AsNoTracking()
            join c in db.Conversations.AsNoTracking() on p.ConversationId equals c.Id
            where p.UserId == request.UserId
            select new ConversationDto(c.Id, c.Type, c.Title)
        ).ToListAsync(cancellationToken);
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
