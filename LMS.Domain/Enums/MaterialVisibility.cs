namespace LMS.Domain.Enums;

/// <summary>
/// Visibility scope for a course material.
///   • Public — every signed-in user can see and download it.
///   • Private — only members (students or the teacher) of the linked
///     classes can. Linkage is stored in <c>material_classes</c>.
/// </summary>
public enum MaterialVisibility
{
    Public = 1,
    Private = 2,
}
