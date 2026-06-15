using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class ClassSession : BaseEntity
{
    public ClassSession(Guid classId, DateOnly sessionDate, TimeOnly startsAt, TimeOnly endsAt, Guid? roomId = null)
    {
        if (classId == Guid.Empty) throw new DomainException("Class id is required.");
        if (startsAt >= endsAt) throw new DomainException("StartsAt must be before EndsAt.");
        ClassId = classId;
        SessionDate = sessionDate;
        StartsAt = startsAt;
        EndsAt = endsAt;
        RoomId = roomId;
    }

    public Guid ClassId { get; private set; }
    public Class? Class { get; private set; }
    public DateOnly SessionDate { get; private set; }
    public TimeOnly StartsAt { get; private set; }
    public TimeOnly EndsAt { get; private set; }
    public Guid? RoomId { get; private set; }
    public Room? Room { get; private set; }

    // ---- Online-lesson details ------------------------------------------
    // What turns a calendar slot into a lesson: an optional topic/title, a
    // Zoom/Google Meet link students join from, and free-form notes. All
    // nullable — a plain in-person slot leaves them blank.
    public string? Topic { get; private set; }
    public string? MeetingUrl { get; private set; }
    public string? Notes { get; private set; }

    public void Reschedule(DateOnly sessionDate, TimeOnly startsAt, TimeOnly endsAt, Guid? roomId)
    {
        if (startsAt >= endsAt) throw new DomainException("StartsAt must be before EndsAt.");
        SessionDate = sessionDate;
        StartsAt = startsAt;
        EndsAt = endsAt;
        RoomId = roomId;
        Touch();
    }

    /// <summary>
    /// Sets the lesson's topic, online meeting link, and notes. Blank/whitespace
    /// values normalise to null so an empty field clears rather than stores "".
    /// </summary>
    public void SetDetails(string? topic, string? meetingUrl, string? notes)
    {
        Topic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim();
        MeetingUrl = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        Touch();
    }
}