namespace LMS.Domain.Enums;

/// <summary>
/// The kind of class-level default content an admin (or the class teacher)
/// attaches to a whole class — the shared "course setup" the teacher then runs.
/// </summary>
public enum ClassResourceKind
{
    /// <summary>Course roadmap / syllabus — long-form plan in <c>Content</c>.</summary>
    Roadmap = 1,

    /// <summary>A video lesson — link in <c>Url</c>.</summary>
    Video = 2,

    /// <summary>An external resource link (PDF, doc, site) — link in <c>Url</c>.</summary>
    Link = 3,

    /// <summary>Default homework / practice the class should do — text in <c>Content</c>, optional <c>Url</c>.</summary>
    Homework = 4
}
