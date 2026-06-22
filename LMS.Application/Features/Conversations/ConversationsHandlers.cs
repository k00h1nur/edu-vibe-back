using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
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
    IRequestHandler<GetMyConversationsQuery, Result<IReadOnlyCollection<ConversationDto>>>,
    IRequestHandler<GetMessageableContactsQuery, Result<IReadOnlyCollection<ContactDto>>>
{
    private bool IsAdmin() =>
        currentUser.IsInRole(RoleCodes.Admin)
        || currentUser.IsInRole(RoleCodes.SuperAdmin)
        || currentUser.IsInRole(RoleCodes.OfficeAdmin);

    /// <summary>
    /// User ids the given user may message. Admin → everyone (minus self).
    /// Otherwise: admins, the teachers of and students in any class the user
    /// teaches or is enrolled in. (Students therefore see classmates + their
    /// teachers + admins; teachers see their class students + admins.)
    /// </summary>
    private async Task<HashSet<Guid>> ResolveMessageableUserIdsAsync(Guid me, bool isAdmin, CancellationToken ct)
    {
        if (isAdmin)
            return (await db.Users.Where(u => u.Id != me).Select(u => u.Id).ToListAsync(ct)).ToHashSet();

        var ids = new HashSet<Guid>();

        // Admin/office staff are always reachable.
        var adminIds = await db.UserRoles
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Code })
            .Where(x => x.Code == RoleCodes.Admin || x.Code == RoleCodes.SuperAdmin || x.Code == RoleCodes.OfficeAdmin)
            .Select(x => x.UserId).Distinct().ToListAsync(ct);
        foreach (var id in adminIds) ids.Add(id);

        // Classes I touch — as teacher (Class.TeacherUserId) or as student (enrollment).
        var taught = await db.Classes.Where(c => c.TeacherUserId == me).Select(c => c.Id).ToListAsync(ct);
        var myProfileId = await db.StudentProfiles.Where(s => s.UserId == me)
            .Select(s => (Guid?)s.Id).FirstOrDefaultAsync(ct);
        var enrolled = myProfileId is { } pid
            ? await db.Enrollments.Where(e => e.StudentProfileId == pid).Select(e => e.ClassId).ToListAsync(ct)
            : new List<Guid>();
        var myClassIds = taught.Concat(enrolled).Distinct().ToList();

        if (myClassIds.Count > 0)
        {
            var teacherIds = await db.Classes
                .Where(c => myClassIds.Contains(c.Id) && c.TeacherUserId != null)
                .Select(c => c.TeacherUserId!.Value).ToListAsync(ct);
            foreach (var id in teacherIds) ids.Add(id);

            var studentIds = await db.Enrollments
                .Where(e => myClassIds.Contains(e.ClassId))
                .Join(db.StudentProfiles, e => e.StudentProfileId, s => s.Id, (e, s) => s.UserId)
                .Distinct().ToListAsync(ct);
            foreach (var id in studentIds) ids.Add(id);
        }

        ids.Remove(me);
        return ids;
    }

    public async Task<Result<IReadOnlyCollection<ContactDto>>> Handle(
        GetMessageableContactsQuery request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } me)
            return Result<IReadOnlyCollection<ContactDto>>.Fail("UNAUTHENTICATED", "Caller must be authenticated.");

        var ids = (await ResolveMessageableUserIdsAsync(me, IsAdmin(), cancellationToken)).ToList();
        if (ids.Count == 0)
            return Result<IReadOnlyCollection<ContactDto>>.Ok(System.Array.Empty<ContactDto>());

        var roles = await db.UserRoles.Where(ur => ids.Contains(ur.UserId))
            .Join(db.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Code })
            .ToListAsync(cancellationToken);
        var staff = await db.StaffProfiles.Where(s => ids.Contains(s.UserId))
            .Select(s => new { s.UserId, s.FirstName, s.LastName }).ToListAsync(cancellationToken);
        var students = await db.StudentProfiles.Where(s => ids.Contains(s.UserId))
            .Select(s => new { s.UserId, s.FirstName, s.LastName }).ToListAsync(cancellationToken);
        var users = await db.Users.Where(u => ids.Contains(u.Id))
            .Select(u => new { u.Id, u.Email }).ToListAsync(cancellationToken);

        string Role(Guid uid)
        {
            var codes = roles.Where(x => x.UserId == uid).Select(x => x.Code).ToList();
            if (codes.Any(c => c is RoleCodes.Admin or RoleCodes.SuperAdmin or RoleCodes.OfficeAdmin)) return "Admin";
            if (codes.Any(c => c is RoleCodes.Teacher or RoleCodes.SupportTeacher)) return "Teacher";
            return "Student";
        }
        string Name(Guid uid)
        {
            var st = staff.FirstOrDefault(x => x.UserId == uid);
            var sp = students.FirstOrDefault(x => x.UserId == uid);
            var full = string.Join(" ", new[] { st?.FirstName ?? sp?.FirstName, st?.LastName ?? sp?.LastName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            return !string.IsNullOrWhiteSpace(full) ? full : users.FirstOrDefault(u => u.Id == uid)?.Email ?? "User";
        }

        var contacts = ids
            .Select(uid => new ContactDto(uid, Name(uid), Role(uid)))
            .OrderBy(c => c.Role).ThenBy(c => c.Name)
            .ToList();
        return Result<IReadOnlyCollection<ContactDto>>.Ok(contacts);
    }

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

        // Scope check: a non-admin may only open a conversation with people they're
        // allowed to message (classmates / their teachers / admins, or — for a
        // teacher — their class students / admins). Admins may message anyone.
        if (currentUser.UserId is { } caller && !IsAdmin())
        {
            var allowed = await ResolveMessageableUserIdsAsync(caller, false, cancellationToken);
            if (request.ParticipantUserIds.Any(id => id != caller && !allowed.Contains(id)))
                return Result<ConversationDto>.Fail("FORBIDDEN",
                    "You can only message people in your classes, your teacher, or an admin.");
        }

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
