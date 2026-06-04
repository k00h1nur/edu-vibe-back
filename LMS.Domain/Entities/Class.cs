using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class Class : BaseEntity
{
    // EF materialisation constructor — fields are populated reflectively by
    // EF Core when reading from the database. Public callers must use the
    // parameterised ctor below, which enforces all invariants.
    private Class()
    {
    }

    public Class(string title, int maxStudents, Modality modality)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Class title is required.");

        if (maxStudents <= 0) throw new DomainException("Max students must be greater than zero.");

        Title = title.Trim();
        MaxStudents = maxStudents;
        Modality = modality;
        Status = ClassStatus.Planned;
    }

    // null! signals to the nullable analyser that the EF materialisation
    // path will populate this — the public ctor always sets it before the
    // entity is observable.
    public string Title { get; private set; } = null!;
    public int MaxStudents { get; private set; }
    public Modality Modality { get; private set; }
    public ClassStatus Status { get; private set; }

    public Guid? TeacherUserId { get; private set; }
    public User? Teacher { get; private set; }

    public ICollection<Enrollment> Enrollments { get; } = new List<Enrollment>();
    public ICollection<Assignment> Assignments { get; } = new List<Assignment>();

    public void EnrollStudent(Guid studentProfileId)
    {
        if (studentProfileId == Guid.Empty) throw new DomainException("Student profile id is required.");

        var activeEnrollmentsCount = Enrollments.Count(x => x.Status == EnrollmentStatus.Active);
        if (activeEnrollmentsCount >= MaxStudents) throw new DomainException("Class is full.");

        if (Enrollments.Any(x => x.StudentProfileId == studentProfileId && x.Status != EnrollmentStatus.Dropped))
            throw new DomainException("Student is already enrolled.");

        Enrollments.Add(Enrollment.Create(Id, studentProfileId, Enrollments));
        Touch();
    }

    public void AssignTeacher(User teacher)
    {
        if (teacher is null) throw new DomainException("Teacher is required.");

        TeacherUserId = teacher.Id;
        Teacher = teacher;
        Touch();
    }

    public void UpdateDetails(string title, int maxStudents, Modality modality)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new DomainException("Class title is required.");
        if (maxStudents <= 0) throw new DomainException("Max students must be greater than zero.");
        if (Status == ClassStatus.Cancelled)
            throw new DomainException("Cannot modify a cancelled class.");

        Title = title.Trim();
        MaxStudents = maxStudents;
        Modality = modality;
        Touch();
    }

    public void Cancel()
    {
        if (Status == ClassStatus.Cancelled) return;
        Status = ClassStatus.Cancelled;
        Touch();
    }
}