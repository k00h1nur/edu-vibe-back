using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class StaffProfile : BaseEntity
{
    public StaffProfile(Guid userId, EmploymentType employmentType)
    {
        if (userId == Guid.Empty) throw new DomainException("User id is required.");
        UserId = userId;
        EmploymentType = employmentType;
    }

    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    public EmploymentType EmploymentType { get; private set; }

    public void SetEmploymentType(EmploymentType employmentType)
    {
        EmploymentType = employmentType;
        Touch();
    }
}