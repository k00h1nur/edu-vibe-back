using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A class-level default content item — the shared "course setup" an admin (or
/// the class teacher) attaches to a whole class: the course roadmap, video
/// lessons, reference links, and default homework. Visible to the class teacher
/// and every enrolled student. Distinct from <see cref="Assignment"/> (graded,
/// per-student submissions) and <see cref="LessonMaterial"/> (files on a single
/// lesson) — this is class-wide, link/text based, and not submitted against.
/// </summary>
public sealed class ClassResource : BaseEntity
{
    // EF materialisation constructor — see Class for the rationale.
    private ClassResource()
    {
    }

    public ClassResource(
        Guid classId,
        ClassResourceKind kind,
        string title,
        string? url,
        string? content,
        Guid createdByUserId)
    {
        if (classId == Guid.Empty) throw new DomainException("Class id is required.");
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Resource title is required.");

        ClassId = classId;
        Kind = kind;
        Title = title.Trim();
        Url = Normalize(url);
        Content = Normalize(content);
        CreatedByUserId = createdByUserId;
    }

    public Guid ClassId { get; private set; }
    public Class? Class { get; private set; }

    public ClassResourceKind Kind { get; private set; }

    public string Title { get; private set; } = null!;

    /// <summary>Link target for <see cref="ClassResourceKind.Video"/> / <see cref="ClassResourceKind.Link"/>.</summary>
    public string? Url { get; private set; }

    /// <summary>Long-form body for <see cref="ClassResourceKind.Roadmap"/> / <see cref="ClassResourceKind.Homework"/>.</summary>
    public string? Content { get; private set; }

    /// <summary>Manual ordering within the class hub (ascending; ties break on <see cref="BaseEntity.CreatedAt"/>).</summary>
    public int Position { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public void Update(ClassResourceKind kind, string title, string? url, string? content)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Resource title is required.");
        Kind = kind;
        Title = title.Trim();
        Url = Normalize(url);
        Content = Normalize(content);
        Touch();
    }

    public void SetPosition(int position)
    {
        Position = position < 0 ? 0 : position;
        Touch();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
