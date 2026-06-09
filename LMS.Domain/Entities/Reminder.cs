using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A simple personal reminder. Owned by the user who created it; visible
/// only to them. Could be extended to per-class / per-student delivery, but
/// the v1 surface is self-only.
/// </summary>
public sealed class Reminder : BaseEntity
{
    private Reminder() { }

    public Reminder(Guid ownerUserId, string title, string? notes, DateTime dueAt)
    {
        if (ownerUserId == Guid.Empty)
            throw new DomainException("Owner user id is required.");
        OwnerUserId = ownerUserId;
        Title = NormalizeTitle(title);
        Notes = NormalizeNotes(notes);
        DueAt = dueAt;
        IsCompleted = false;
    }

    public Guid OwnerUserId { get; private set; }
    public User? OwnerUser { get; private set; }

    public string Title { get; private set; } = null!;
    public string? Notes { get; private set; }
    public DateTime DueAt { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    public void UpdateContent(string title, string? notes, DateTime dueAt)
    {
        Title = NormalizeTitle(title);
        Notes = NormalizeNotes(notes);
        DueAt = dueAt;
        Touch();
    }

    public void MarkCompleted()
    {
        if (IsCompleted) return;
        IsCompleted = true;
        CompletedAt = DateTime.UtcNow;
        Touch();
    }

    public void MarkPending()
    {
        if (!IsCompleted) return;
        IsCompleted = false;
        CompletedAt = null;
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

    private static string? NormalizeNotes(string? notes)
    {
        if (string.IsNullOrWhiteSpace(notes)) return null;
        var trimmed = notes.Trim();
        if (trimmed.Length > 2000)
            throw new DomainException("Notes must be 2000 characters or fewer.");
        return trimmed;
    }
}
