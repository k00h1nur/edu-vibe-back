using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// M:N link between assignments and their referenced books. Carries an
/// optional <c>Note</c> so the teacher can hint at which chapter / pages
/// the students should focus on.
/// </summary>
public sealed class AssignmentBook : BaseEntity
{
    private AssignmentBook() { }

    public AssignmentBook(Guid assignmentId, Guid bookId, string? note = null)
    {
        if (assignmentId == Guid.Empty) throw new DomainException("Assignment id is required.");
        if (bookId == Guid.Empty) throw new DomainException("Book id is required.");
        AssignmentId = assignmentId;
        BookId = bookId;
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }

    public Guid AssignmentId { get; private set; }
    public Assignment? Assignment { get; private set; }

    public Guid BookId { get; private set; }
    public Book? Book { get; private set; }

    public string? Note { get; private set; }

    public void SetNote(string? note)
    {
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        Touch();
    }
}
