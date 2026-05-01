using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class XpLedger : BaseEntity
{
    private XpLedger(Guid studentProfileId, int amount, XpSourceType sourceType, string? note)
    {
        if (studentProfileId == Guid.Empty) throw new DomainException("Student profile id is required.");

        if (amount == 0) throw new DomainException("XP amount cannot be zero.");

        StudentProfileId = studentProfileId;
        Amount = amount;
        SourceType = sourceType;
        Note = note?.Trim();
    }

    public Guid StudentProfileId { get; private set; }
    public StudentProfile? StudentProfile { get; private set; }

    public int Amount { get; private set; }
    public XpSourceType SourceType { get; private set; }
    public string? Note { get; private set; }

    public static XpLedger CreateEntry(Guid studentProfileId, int amount, XpSourceType sourceType, string? note = null)
    {
        return new XpLedger(studentProfileId, amount, sourceType, note);
    }
}