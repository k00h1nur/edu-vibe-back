using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// Join row between <see cref="StaffProfile"/> and <see cref="Specialization"/>.
/// Many-to-many: a teacher can list multiple specializations and a
/// specialization can have many teachers.
/// </summary>
public sealed class StaffSpecialization : BaseEntity
{
    private StaffSpecialization() { }

    public StaffSpecialization(Guid staffProfileId, Guid specializationId)
    {
        if (staffProfileId == Guid.Empty)
            throw new DomainException("Staff profile id is required.");
        if (specializationId == Guid.Empty)
            throw new DomainException("Specialization id is required.");
        StaffProfileId = staffProfileId;
        SpecializationId = specializationId;
    }

    public Guid StaffProfileId { get; private set; }
    public StaffProfile? StaffProfile { get; private set; }

    public Guid SpecializationId { get; private set; }
    public Specialization? Specialization { get; private set; }
}
