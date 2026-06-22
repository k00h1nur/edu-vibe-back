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
        // New lessons are published by default so creating a slot keeps the
        // existing "everything is visible" behaviour; teachers explicitly
        // unpublish or schedule to hide content.
        IsPublished = true;
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

    /// <summary>
    /// Optional video lesson — a YouTube/Vimeo/MP4 URL students watch from the
    /// lesson hub. Embedded on the session (1:1) rather than a separate entity.
    /// </summary>
    public string? VideoUrl { get; private set; }

    /// <summary>One-to-many lesson files attached to this session.</summary>
    public ICollection<LessonMaterial> Materials { get; } = new List<LessonMaterial>();

    /// <summary>
    /// The curriculum lesson this scheduled session teaches (null = ad-hoc
    /// session not bound to a curriculum). This is the schedule↔curriculum link:
    /// it lets "today's topic" resolve the module / unit / objective for any date.
    /// </summary>
    public Guid? CurriculumLessonId { get; private set; }
    public CurriculumLesson? CurriculumLesson { get; private set; }

    /// <summary>
    /// Binds this session to a curriculum lesson and (optionally) copies the
    /// lesson topic onto the session's Topic. Pass null to unbind.
    /// </summary>
    public void LinkCurriculumLesson(Guid? curriculumLessonId, string? topic = null)
    {
        CurriculumLessonId = curriculumLessonId;
        if (!string.IsNullOrWhiteSpace(topic)) Topic = topic.Trim();
        Touch();
    }

    // ---- Lesson content publishing --------------------------------------
    // Controls whether the lesson's CONTENT (notes, video, materials, the hub
    // itself) is visible to students. A slot can exist on the timetable while
    // its content is withheld (unpublished) or time-boxed (visibility window).
    public bool IsPublished { get; private set; }
    /// <summary>When the lesson was first published (audit/info). Null while unpublished.</summary>
    public DateTime? PublishedAt { get; private set; }
    /// <summary>Content hidden from students before this instant (null = no lower bound).</summary>
    public DateTime? VisibleFrom { get; private set; }
    /// <summary>Content hidden from students after this instant (null = no upper bound).</summary>
    public DateTime? VisibleUntil { get; private set; }

    /// <summary>Publish or unpublish the lesson content. Stamps PublishedAt on first publish.</summary>
    public void SetPublished(bool published, DateTime now)
    {
        if (published && PublishedAt is null) PublishedAt = now;
        IsPublished = published;
        Touch();
    }

    /// <summary>Schedules the visibility window. Either bound may be null (open-ended).</summary>
    public void SetVisibilityWindow(DateTime? visibleFrom, DateTime? visibleUntil)
    {
        if (visibleFrom is not null && visibleUntil is not null && visibleFrom > visibleUntil)
            throw new DomainException("VisibleFrom must be on or before VisibleUntil.");
        VisibleFrom = visibleFrom;
        VisibleUntil = visibleUntil;
        Touch();
    }

    /// <summary>
    /// True when the content should be visible to a student at <paramref name="now"/>:
    /// published AND inside the (optional) visibility window. Teachers/staff bypass this.
    /// Mirror the predicate inline in LINQ-to-entities queries (it can't be translated).
    /// </summary>
    public bool IsVisibleToStudents(DateTime now) =>
        IsPublished
        && (VisibleFrom is null || VisibleFrom <= now)
        && (VisibleUntil is null || now <= VisibleUntil);

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

    /// <summary>Sets or clears the video-lesson URL. Blank normalises to null.</summary>
    public void SetVideo(string? videoUrl)
    {
        VideoUrl = string.IsNullOrWhiteSpace(videoUrl) ? null : videoUrl.Trim();
        Touch();
    }
}