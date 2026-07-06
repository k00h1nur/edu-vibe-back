using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Notifications;

/// <summary>
/// Builds the in-app notification feed for the current user. Read-only and
/// self-scoped: it only ever reads things addressed to the caller (unread
/// messages where they're a participant) and, for staff, the shared inquiry
/// inbox. No new storage — it projects existing rows, so it stays in lockstep
/// with the Messages + VisitorMessages features automatically.
/// </summary>
public sealed class NotificationsHandlers(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetNotificationsQuery, Result<IReadOnlyCollection<NotificationDto>>>
{
    public async Task<Result<IReadOnlyCollection<NotificationDto>>> Handle(
        GetNotificationsQuery request, CancellationToken ct)
    {
        var uid = currentUser.UserId;
        if (uid is null)
            return Result<IReadOnlyCollection<NotificationDto>>.Fail("FORBIDDEN", "Sign in required.");

        var take = Math.Clamp(request.Take, 1, 50);
        var feed = new List<NotificationDto>();

        // 1) Unread direct messages addressed to the caller.
        var msgs = await db.Messages.AsNoTracking()
            .Where(m => m.SenderUserId != uid.Value
                        && m.ReadAt == null
                        && db.ConversationParticipants.Any(
                            p => p.ConversationId == m.ConversationId && p.UserId == uid.Value))
            .OrderByDescending(m => m.CreatedAt)
            .Take(take)
            .Select(m => new { m.Id, m.SenderUserId, m.Text, m.CreatedAt })
            .ToListAsync(ct);

        if (msgs.Count > 0)
        {
            var senderIds = msgs.Select(m => m.SenderUserId).Distinct().ToList();
            var names = await ResolveNamesAsync(senderIds, ct);
            foreach (var m in msgs)
            {
                names.TryGetValue(m.SenderUserId, out var name);
                feed.Add(new NotificationDto(
                    $"message:{m.Id}", "message",
                    string.IsNullOrWhiteSpace(name) ? "New message" : name,
                    m.Text.Length > 120 ? m.Text[..120] + "…" : m.Text,
                    m.CreatedAt));
            }
        }

        // 2) New visitor inquiries — staff only (mirrors who can open the
        //    Inquiries inbox). Demo lesson / mock test / level check / contact.
        var isStaff = currentUser.IsAdmin();
        if (isStaff)
        {
            var inquiries = await db.VisitorMessages.AsNoTracking()
                .Where(v => !v.IsRead)
                .OrderByDescending(v => v.CreatedAt)
                .Take(take)
                .Select(v => new { v.Id, v.Name, v.Phone, v.Source, v.CreatedAt })
                .ToListAsync(ct);
            foreach (var v in inquiries)
                feed.Add(new NotificationDto(
                    $"inquiry:{v.Id}", "inquiry",
                    SourceLabel(v.Source),
                    string.IsNullOrWhiteSpace(v.Phone) ? v.Name : $"{v.Name} · {v.Phone}",
                    v.CreatedAt));
        }

        var ordered = feed
            .OrderByDescending(n => n.CreatedAt)
            .Take(take)
            .ToList();
        return Result<IReadOnlyCollection<NotificationDto>>.Ok(ordered);
    }

    /// <summary>Batch-resolves display names for a set of user ids (staff → student → email local-part).</summary>
    private async Task<Dictionary<Guid, string>> ResolveNamesAsync(List<Guid> userIds, CancellationToken ct)
    {
        var map = new Dictionary<Guid, string>();

        var staff = await db.StaffProfiles.AsNoTracking()
            .Where(s => userIds.Contains(s.UserId))
            .Select(s => new { s.UserId, Name = ((s.FirstName ?? "") + " " + (s.LastName ?? "")).Trim() })
            .ToListAsync(ct);
        foreach (var s in staff)
            if (!string.IsNullOrWhiteSpace(s.Name)) map[s.UserId] = s.Name;

        var students = await db.StudentProfiles.AsNoTracking()
            .Where(s => userIds.Contains(s.UserId))
            .Select(s => new { s.UserId, Name = ((s.FirstName ?? "") + " " + (s.LastName ?? "")).Trim() })
            .ToListAsync(ct);
        foreach (var s in students)
            if (!map.ContainsKey(s.UserId) && !string.IsNullOrWhiteSpace(s.Name)) map[s.UserId] = s.Name;

        var missing = userIds.Where(id => !map.ContainsKey(id)).ToList();
        if (missing.Count > 0)
        {
            var emails = await db.Users.AsNoTracking()
                .Where(u => missing.Contains(u.Id))
                .Select(u => new { u.Id, u.Email })
                .ToListAsync(ct);
            foreach (var e in emails)
            {
                var label = string.IsNullOrWhiteSpace(e.Email)
                    ? "Someone"
                    : (e.Email.Contains('@') ? e.Email[..e.Email.IndexOf('@')] : e.Email);
                map[e.Id] = label;
            }
        }

        return map;
    }

    private static string SourceLabel(VisitorMessageSource source) => source switch
    {
        VisitorMessageSource.DemoLesson => "Demo lesson request",
        VisitorMessageSource.MockTest   => "Mock test registration",
        VisitorMessageSource.LevelCheck => "Level check request",
        VisitorMessageSource.Contact    => "Contact message",
        _                               => "New inquiry",
    };
}
