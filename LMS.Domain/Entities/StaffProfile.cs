using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class StaffProfile : BaseEntity
{
    private StaffProfile() { }

    public StaffProfile(Guid userId, EmploymentType employmentType)
    {
        if (userId == Guid.Empty) throw new DomainException("User id is required.");
        UserId = userId;
        EmploymentType = employmentType;
    }

    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    public EmploymentType EmploymentType { get; private set; }

    // Office Admin surface: editable profile fields. Mirror of StudentProfile —
    // every column nullable so the migration is safe over existing rows.
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? PhoneNumber { get; private set; }
    public string? Description { get; private set; }

    /// <summary>
    /// Teaching specializations (Math, Physics, Chinese, …). Admin-curated
    /// list (see <see cref="Specialization"/>) — the teacher picks one or
    /// more from their profile page.
    /// </summary>
    public ICollection<StaffSpecialization> Specializations { get; } = new List<StaffSpecialization>();

    public void SetEmploymentType(EmploymentType employmentType)
    {
        EmploymentType = employmentType;
        Touch();
    }

    /// <summary>
    /// Updates the editable profile fields. All four arguments are always
    /// passed (no partial-update form) so the caller controls clearing.
    /// </summary>
    public void UpdateProfile(string? firstName, string? lastName, string? phoneNumber, string? description)
    {
        FirstName = NormalizeOrNull(firstName, maxLength: 128, fieldName: nameof(FirstName));
        LastName = NormalizeOrNull(lastName, maxLength: 128, fieldName: nameof(LastName));
        PhoneNumber = NormalizeOrNull(phoneNumber, maxLength: 32, fieldName: nameof(PhoneNumber));
        Description = NormalizeOrNull(description, maxLength: 2000, fieldName: nameof(Description));
        Touch();
    }

    private static string? NormalizeOrNull(string? value, int maxLength, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new DomainException($"{fieldName} must be {maxLength} characters or fewer.");
        return trimmed;
    }
}
