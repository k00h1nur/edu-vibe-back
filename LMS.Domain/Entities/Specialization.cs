using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A teaching specialization (Math, Physics, Chinese, English, …). Managed
/// by admins so the academy can curate the list; teachers pick from it on
/// their profile. Many-to-many with <see cref="StaffProfile"/> via
/// <see cref="StaffSpecialization"/>.
/// </summary>
public sealed class Specialization : BaseEntity
{
    // EF materialisation ctor — public ctor below enforces invariants.
    private Specialization() { }

    public Specialization(string code, string name)
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        IsActive = true;
    }

    /// <summary>Stable lookup code — lowercase, hyphenated, unique. e.g. "math", "general-english".</summary>
    public string Code { get; private set; } = null!;

    /// <summary>Display name — shown to teachers and on the marketing site.</summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Soft-disabled specializations stay in the table so existing
    /// StaffSpecialization rows resolve, but they no longer appear in the
    /// teacher selector.
    /// </summary>
    public bool IsActive { get; private set; }

    public ICollection<StaffSpecialization> StaffLinks { get; } = new List<StaffSpecialization>();

    public void Rename(string name)
    {
        Name = NormalizeName(name);
        Touch();
    }

    public void Activate()
    {
        if (IsActive) return;
        IsActive = true;
        Touch();
    }

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        Touch();
    }

    private static string NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Specialization code is required.");
        var trimmed = code.Trim().ToLowerInvariant();
        if (trimmed.Length > 64)
            throw new DomainException("Specialization code must be 64 characters or fewer.");
        return trimmed;
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Specialization name is required.");
        var trimmed = name.Trim();
        if (trimmed.Length > 128)
            throw new DomainException("Specialization name must be 128 characters or fewer.");
        return trimmed;
    }
}
