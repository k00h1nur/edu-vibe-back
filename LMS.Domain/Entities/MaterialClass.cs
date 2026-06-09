using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// Join row between <see cref="Material"/> and <see cref="Class"/> used when
/// the material's visibility is Private. Members of the linked class can read
/// the material; everyone else gets 403.
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
