using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A bookable mock-test session shown on the marketing site's Mock Test page.
/// Editable from the admin Mock Tests page (Marketing.Manage), surfaced
/// anonymously at GET /api/MockTestSlots/public (active + upcoming only).
/// Booking itself still flows through VisitorMessages (the inquiry form).
/// </summary>
public sealed class MockTestSlot : BaseEntity
{
    private MockTestSlot() { }

    public MockTestSlot(
        string title,
        DateTime startsAtUtc,
        string? durationText,
        int capacity,
        int availableSeats,
        int sortOrder,
        bool isActive)
    {
        Title = NormalizeTitle(title);
        StartsAt = startsAtUtc;
        DurationText = Trim(durationText, 64);
        Capacity = capacity < 0 ? 0 : capacity;
        AvailableSeats = ClampSeats(availableSeats, Capacity);
        SortOrder = sortOrder;
        IsActive = isActive;
    }

    public string Title { get; private set; } = null!;
    /// <summary>The session's scheduled start (UTC). Public list filters to future slots.</summary>
    public DateTime StartsAt { get; private set; }
    /// <summary>Free-form length, e.g. "2h 45min".</summary>
    public string? DurationText { get; private set; }
    /// <summary>Total seats for the session.</summary>
    public int Capacity { get; private set; }
    /// <summary>Seats still open — what the marketing card shows.</summary>
    public int AvailableSeats { get; private set; }
    /// <summary>Lower numbers float to the top within the same date.</summary>
    public int SortOrder { get; private set; }
    public bool IsActive { get; private set; }

    public void Update(
        string title, DateTime startsAtUtc, string? durationText,
        int capacity, int availableSeats, int sortOrder, bool isActive)
    {
        Title = NormalizeTitle(title);
        StartsAt = startsAtUtc;
        DurationText = Trim(durationText, 64);
        Capacity = capacity < 0 ? 0 : capacity;
        AvailableSeats = ClampSeats(availableSeats, Capacity);
        SortOrder = sortOrder;
        IsActive = isActive;
        Touch();
    }

    private static string NormalizeTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Mock test title is required.");
        var t = title.Trim();
        if (t.Length > 256)
            throw new DomainException("Mock test title must be 256 characters or fewer.");
        return t;
    }

    private static int ClampSeats(int seats, int capacity)
    {
        if (seats < 0) return 0;
        return capacity > 0 && seats > capacity ? capacity : seats;
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim();
        return t.Length > max ? t[..max] : t;
    }
}
