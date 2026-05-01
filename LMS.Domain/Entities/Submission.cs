using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class Submission : BaseEntity
{
    private Submission(Guid assignmentId, Guid studentProfileId, string content)
    {
        if (assignmentId == Guid.Empty || studentProfileId == Guid.Empty)
            throw new DomainException("Assignment and student profile ids are required.");

        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Submission content is required.");

        AssignmentId = assignmentId;
        StudentProfileId = studentProfileId;
        Content = content.Trim();
        Status = SubmissionStatus.Submitted;
    }

    public Guid AssignmentId { get; }
    public Assignment? Assignment { get; private set; }

    public Guid StudentProfileId { get; }
    public StudentProfile? StudentProfile { get; private set; }

    public string Content { get; private set; }
    public SubmissionStatus Status { get; private set; }
    public decimal? Score { get; private set; }

    public static Submission Create(Guid assignmentId, Guid studentProfileId, string content,
        IEnumerable<Submission> existingSubmissions)
    {
        if (existingSubmissions.Any(x => x.AssignmentId == assignmentId && x.StudentProfileId == studentProfileId))
            throw new DomainException("Only one submission is allowed per student per assignment.");

        return new Submission(assignmentId, studentProfileId, content);
    }

    public void Submit(string content, bool isLate = false)
    {
        if (string.IsNullOrWhiteSpace(content)) throw new DomainException("Submission content is required.");

        Content = content.Trim();
        Status = isLate ? SubmissionStatus.Late : SubmissionStatus.Submitted;
        Touch();
    }

    public void Grade(decimal score)
    {
        if (score < 0) throw new DomainException("Score cannot be negative.");

        Score = score;
        Status = SubmissionStatus.Graded;
        Touch();
    }
}