namespace LMS.Domain.Enums;

/// <summary>
/// Visibility level for a <see cref="Entities.Material"/>.
///   - <see cref="Public"/>: every signed-in user can read.
///   - <see cref="Private"/>: only members of the linked classes can read.
/// </summary>
public enum MaterialVisibility
{
    Public = 1,
    Private = 2,
}
