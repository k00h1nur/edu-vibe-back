using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// Academy-wide announcement. Surfaces on the student dashboard widget and
/// (when <see cref="IsPublic"/>) on the marketing site's news strip.
/// Authored from the admin Announcements page.
///
/// Visibility:
///   • IsPublic = true  → marketing site + every signed-in user.
///   • IsPublic = false → signed-in users only (the default).
/// </summary>
public sealed class Announcement : BaseEntity
{
    private Announcement() { }

    public Announcement(
        string title,
        string body,
        bool isPublic,
        DateTime? publishesAt,
        DateTime? expiresAt,
        Guid authorUserId)
    {
        Title = NormalizeTitle(title);
        Body = NormalizeBody(body);
        IsPublic = isPublic;
        PublishesAt = publishesAt;
        ExpiresAt = expiresAt;
        if (authorUserId == Guid.Empty)
            throw new DomainException("Author user id is required.");
        AuthorUserId = authorUserId;
    }

    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public bool IsPublic { get; private set; }

    /// <summary>When the announcement starts being visible. Null = immediately.</summary>
    public DateTime? PublishesAt { get; private set; }
    /// <summary>When the announcement stops being visible. Null = no expiry.</summary>
    public DateTime? ExpiresAt { get; private set; }

    public Guid AuthorUserId { get; private set; }
    public User? AuthorUser { get; private set; }

    public void Update(string title, string body, bool isPublic, DateTime? publishesAt, DateTime? expiresAt)
    {
        Title = NormalizeTitle(title);
        Body = NormalizeBody(body);
        IsPublic = isPublic;
        PublishesAt = publishesAt;
        ExpiresAt = expiresAt;
        Touch();
    }

    /// <summary>Whether the announcement is currently visible at <paramref name="now"/>.</summary>
    public bool IsLiveAt(DateTime now)
    {
        if (PublishesAt is { } start && now < start) return false;
        if (ExpiresAt is { } end && now > end) return false;
        return true;
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required.");
        var t = title.Trim();
        if (t.Length > 256)
            throw new DomainException("Title must be 256 characters or fewer.");
        return t;
    }

    private static string NormalizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("Body is required.");
        var b = body.Trim();
        if (b.Length > 4000)
            throw new DomainException("Body must be 4000 characters or fewer.");
        return b;
    }
}
