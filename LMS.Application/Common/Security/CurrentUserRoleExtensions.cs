using LMS.Application.Common.Abstractions;

namespace LMS.Application.Common.Security;

/// <summary>
/// Role-group checks on the current user. Centralises the "is this caller an
/// admin / teacher / staff member?" question that handlers used to answer with
/// ad-hoc lists of <see cref="ICurrentUserService.IsInRole"/> calls — some using
/// PascalCase <see cref="RoleCodes"/>, some lowercase literals (the latter only
/// worked because IsInRole is case-insensitive). Matching still goes through
/// IsInRole, so it stays case-insensitive against the JWT role claims.
/// </summary>
public static class CurrentUserRoleExtensions
{
    /// <summary>Caller holds any admin-level role (see <see cref="RoleGroups.Admin"/>).</summary>
    public static bool IsAdmin(this ICurrentUserService user) => user.IsInAnyRole(RoleGroups.Admin);

    /// <summary>Caller holds a teaching role (see <see cref="RoleGroups.Teacher"/>).</summary>
    public static bool IsTeacher(this ICurrentUserService user) => user.IsInAnyRole(RoleGroups.Teacher);

    /// <summary>Caller is any staff member — admin-level or teaching (see <see cref="RoleGroups.Staff"/>).</summary>
    public static bool IsStaff(this ICurrentUserService user) => user.IsInAnyRole(RoleGroups.Staff);

    /// <summary>Caller is a student.</summary>
    public static bool IsStudent(this ICurrentUserService user) => user.IsInRole(RoleCodes.Student);

    /// <summary>True when the caller holds at least one of the supplied roles (case-insensitive).</summary>
    public static bool IsInAnyRole(this ICurrentUserService user, IEnumerable<string> roles) =>
        roles.Any(user.IsInRole);
}
