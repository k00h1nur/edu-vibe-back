using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// Join row between a private <see cref="Material"/> and a <see cref="Class"/>
/// whose members are allowed to see it. Has no extra data — its presence is
/// the grant.
/// </summary>
public sealed class MaterialClass : BaseEntity
{
    private MaterialClass() { }

    public MaterialClass(Guid materialId, Guid classId)
    {
        if (materialId == Guid.Empty) throw new DomainException("Material id is required.");
        if (classId == Guid.Empty) throw new DomainException("Class id is required.");
        MaterialId = materialId;
        ClassId = classId;
    }

    public Guid MaterialId { get; private set; }
    public Material? Material { get; private set; }

    public Guid ClassId { get; private set; }
    public Class? Class { get; private set; }
}
