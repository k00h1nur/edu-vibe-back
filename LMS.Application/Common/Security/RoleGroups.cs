namespace LMS.Application.Common.Security;

/// <summary>
/// Named groupings of <see cref="RoleCodes"/> for authorization checks, so
/// "who counts as an admin / teacher / staff member" is defined ONCE instead of
/// being re-listed — with subtle drift — in every handler.
///
/// Includes the legacy codes (SuperAdmin / OfficeAdmin / AcademyDirector /
/// SupportTeacher) for backward-compat with historical role rows. No user is
/// assigned them anymore — only Admin / Teacher / Student are seeded — so their
/// presence here is inert for live users while keeping any old assignments
/// working. Before this type, handlers listed these sets inconsistently (some
/// omitted AcademyDirector, one omitted SuperAdmin); unifying is a no-op for the
/// three live roles.
/// </summary>
public static class RoleGroups
{
    /// <summary>Admin-level authority (full platform management).</summary>
    public static readonly string[] Admin =
    {
        RoleCodes.Admin, RoleCodes.SuperAdmin, RoleCodes.OfficeAdmin, RoleCodes.AcademyDirector,
    };

    /// <summary>Teaching authority.</summary>
    public static readonly string[] Teacher =
    {
        RoleCodes.Teacher, RoleCodes.SupportTeacher,
    };

    /// <summary>Any staff member — admin-level or teaching (i.e. not a student).</summary>
    public static readonly string[] Staff =
    {
        RoleCodes.Admin, RoleCodes.SuperAdmin, RoleCodes.OfficeAdmin, RoleCodes.AcademyDirector,
        RoleCodes.Teacher, RoleCodes.SupportTeacher,
    };
}
