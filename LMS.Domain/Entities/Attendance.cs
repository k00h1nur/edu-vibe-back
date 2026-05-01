using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class Attendance : BaseEntity
{
    private Attendance(Guid classId, Guid sessionId, Guid studentProfileId)
    {
        if (classId == Guid.Empty || sessionId == Guid.Empty || studentProfileId == Guid.Empty)
            throw new DomainException("Class, session and student profile ids are required.");

        ClassId = classId;
        SessionId = sessionId;
        StudentProfileId = studentProfileId;
        Status = AttendanceStatus.Absent;
    }

    public Guid ClassId { get; private set; }
    public Class? Class { get; private set; }

    public Guid SessionId { get; }
    public Guid StudentProfileId { get; }
    public StudentProfile? StudentProfile { get; private set; }

    public AttendanceStatus Status { get; private set; }

    public static Attendance Create(Guid classId, Guid sessionId, Guid studentProfileId,
        IEnumerable<Attendance> existingAttendances)
    {
        if (existingAttendances.Any(x => x.SessionId == sessionId && x.StudentProfileId == studentProfileId))
            throw new DomainException("Attendance must be unique per session and student.");

        return new Attendance(classId, sessionId, studentProfileId);
    }

    public void Mark(AttendanceStatus status)
    {
        Status = status;
        Touch();
    }
}