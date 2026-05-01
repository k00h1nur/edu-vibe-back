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
}