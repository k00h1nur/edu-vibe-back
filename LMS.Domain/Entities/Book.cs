using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// Reference material that teachers can attach to assignments. The actual
/// PDF / EPUB lives in object storage; the entity holds the URL and metadata
/// used by the catalog and assignment views.
/// </summary>
public sealed class Book : BaseEntity
{
    private Book() { }

    public Book(string title, string? author, string? subject, string? level,
        string? description, string? coverImageUrl, string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Book title is required.");
        Title = title.Trim();
        Author = string.IsNullOrWhiteSpace(author) ? null : author.Trim();
        Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();
        Level = string.IsNullOrWhiteSpace(level) ? null : level.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CoverImageUrl = string.IsNullOrWhiteSpace(coverImageUrl) ? null : coverImageUrl.Trim();
        FileUrl = string.IsNullOrWhiteSpace(fileUrl) ? null : fileUrl.Trim();
    }

    public string Title { get; private set; } = string.Empty;
    public string? Author { get; private set; }
    /// <summary>Free-form subject tag (e.g. "English", "Math", "IELTS").</summary>
    public string? Subject { get; private set; }
    /// <summary>CEFR level or grade (e.g. "B2", "Grade 10").</summary>
    public string? Level { get; private set; }
    public string? Description { get; private set; }
    public string? CoverImageUrl { get; private set; }
    public string? FileUrl { get; private set; }

    public ICollection<AssignmentBook> AssignmentLinks { get; } = new List<AssignmentBook>();

    public void Update(string title, string? author, string? subject, string? level,
        string? description, string? coverImageUrl, string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Book title is required.");
        Title = title.Trim();
        Author = string.IsNullOrWhiteSpace(author) ? null : author.Trim();
        Subject = string.IsNullOrWhiteSpace(subject) ? null : subject.Trim();
        Level = string.IsNullOrWhiteSpace(level) ? null : level.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        CoverImageUrl = string.IsNullOrWhiteSpace(coverImageUrl) ? null : coverImageUrl.Trim();
        FileUrl = string.IsNullOrWhiteSpace(fileUrl) ? null : fileUrl.Trim();
        Touch();
    }
}
