using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A short announcement shown on the student dashboard and (optionally) the
/// marketing landing. Admin publishes, students read. Soft visibility via
/// <see cref="IsPublished"/> so draft text can be saved without leaking.
/// </summary>
public sealed class Announcement : BaseEntity
{
    private Announcement() { }

    public Announcement(string title, string body, Guid authorUserId)
    {
        Title = NormalizeTitle(title);
        Body = NormalizeBody(body);
        if (authorUserId == Guid.Empty) throw new DomainException("Author user id is required.");
        AuthorUserId = authorUserId;
        IsPublished = false;
    }

    public string Title { get; private set; } = null!;
    public string Body { get; private set; } = null!;
    public bool IsPublished { get; private set; }
    public DateTime? PublishedAt { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public User? AuthorUser { get; private set; }

    public void UpdateContent(string title, string body)
    {
        Title = NormalizeTitle(title);
        Body = NormalizeBody(body);
        Touch();
    }

    public void Publish()
    {
        if (IsPublished) return;
        IsPublished = true;
        PublishedAt = DateTime.UtcNow;
        Touch();
    }

    public void Unpublish()
    {
        if (!IsPublished) return;
        IsPublished = false;
        Touch();
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Title is required.");
        var trimmed = title.Trim();
        if (trimmed.Length > 256)
            throw new DomainException("Title must be 256 characters or fewer.");
        return trimmed;
    }

    private static string NormalizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("Body is required.");
        var trimmed = body.Trim();
        if (trimmed.Length > 4000)
            throw new DomainException("Body must be 4000 characters or fewer.");
        return trimmed;
    }
}
