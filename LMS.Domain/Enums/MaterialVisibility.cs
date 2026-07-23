namespace LMS.Domain.Enums;

/// <summary>
/// Visibility scope for a course material. A material targets exactly one
/// audience:
///   • Public — every signed-in user can see and download it.
///   • Private — only members (students or the teacher) of the linked
///     classes can. Linkage is stored in <c>material_classes</c>.
///   • AdminsOnly — only admin-level staff (admin / director / office admin).
///   • TeachersOnly — teaching staff (plus admins, who see everything).
///   • StudentsOnly — students (plus admins).
///
/// Role-scoped values (Admins/Teachers/Students) need no class links; the
/// caller's role alone decides visibility — see
/// <c>MaterialsHandlers.VisibleMaterialIds</c>. Stored as the integer value,
/// so new members are additive with no schema migration.
/// </summary>
public enum MaterialVisibility
{
    Public = 1,
    Private = 2,
    AdminsOnly = 3,
    TeachersOnly = 4,
    StudentsOnly = 5,
}
