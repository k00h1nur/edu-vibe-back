using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class Assignment : BaseEntity
{
    private Assignment()
    {
    }
    public Assignment(Guid classId, string title, User createdByTeacher)
    {
        if (classId == Guid.Empty) throw new DomainException("Class id is required.");

        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Assignment title is required.");

        if (createdByTeacher is null) throw new DomainException("Teacher is required.");

        ClassId = classId;
        Title = title.Trim();
        CreatedByTeacherId = createdByTeacher.Id;
        Status = AssignmentStatus.Draft;
    }

    public Guid ClassId { get; private set; }
    public Class? Class { get; private set; }

    public string Title { get; private set; }
    public AssignmentStatus Status { get; private set; }

    public Guid CreatedByTeacherId { get; private set; }
    public User? CreatedByTeacher { get; private set; }

    public ICollection<Submission> Submissions { get; } = new List<Submission>();

    public void Publish()
    {
        if (Status != AssignmentStatus.Draft) throw new DomainException("Only draft assignment can be published.");

        Status = AssignmentStatus.Published;
        Touch();
    }

    public void Close()
    {
        if (Status == AssignmentStatus.Closed) throw new DomainException("Assignment is already closed.");

        Status = AssignmentStatus.Closed;
        Touch();
    }
}