namespace LMS.Application.Common.Security;

/// <summary>
/// Backend role code constants.
///
/// The platform has consolidated to three primary roles
/// (<see cref="Admin"/>, <see cref="Teacher"/>, <see cref="Student"/>). The
/// four legacy codes (SuperAdmin / AcademyDirector / OfficeAdmin /
/// SupportTeacher) still exist for backward-compat with historical user
/// assignments and a few solution-strip allowlists, but new code should not
/// grant them. See <c>DemoUsersSeederHostedService</c> and
/// <c>RolePermissionSeederHostedService</c> — neither seeds the legacy four.
/// </summary>
public static class RoleCodes
{
    public const string Admin = "Admin";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    // Legacy — kept for backward compat with historical role rows.
    public const string SuperAdmin = "SuperAdmin";
    public const string AcademyDirector = "AcademyDirector";
    public const string OfficeAdmin = "OfficeAdmin";
    public const string SupportTeacher = "SupportTeacher";
}
