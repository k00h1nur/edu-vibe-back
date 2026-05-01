namespace LMS.Application.Common.Security;

public static class PermissionMatrix
{
    private static readonly string[] AllPermissions =
    [
        Permissions.UsersManage, Permissions.StaffManage, Permissions.StudentsRegister, Permissions.StudentsEdit,
        Permissions.StudentsViewAll, Permissions.CoursesManage, Permissions.RoomsManage, Permissions.ClassesManage,
        Permissions.ClassesViewAssigned, Permissions.SessionsManage, Permissions.AttendanceMark,
        Permissions.AttendanceView,
        Permissions.AssignmentsCreate, Permissions.AssignmentsGrade, Permissions.SubmissionsSubmitOwn,
        Permissions.PaymentsManage,
        Permissions.PaymentsView, Permissions.ConversationsCreate, Permissions.MessagesSend, Permissions.BadgesManage,
        Permissions.XpManage, Permissions.AnalyticsViewAll, Permissions.AnalyticsViewAssigned,
        Permissions.AnalyticsViewOwn,
        Permissions.ReportsExport
    ];

    private static readonly Dictionary<string, string[]> RolePermissions = new(StringComparer.OrdinalIgnoreCase)
    {
        [RoleCodes.OfficeAdmin] =
        [
            Permissions.StudentsRegister, Permissions.StudentsEdit, Permissions.StudentsViewAll,
            Permissions.ClassesManage,
            Permissions.SessionsManage, Permissions.RoomsManage, Permissions.PaymentsManage, Permissions.PaymentsView,
            Permissions.AttendanceView, Permissions.ConversationsCreate, Permissions.MessagesSend,
            Permissions.AnalyticsViewAssigned, Permissions.ReportsExport
        ],
        [RoleCodes.Teacher] =
        [
            Permissions.StudentsViewAll, Permissions.ClassesManage, Permissions.ClassesViewAssigned,
            Permissions.SessionsManage,
            Permissions.AttendanceMark, Permissions.AttendanceView, Permissions.AssignmentsCreate,
            Permissions.AssignmentsGrade,
            Permissions.ConversationsCreate, Permissions.MessagesSend, Permissions.BadgesManage, Permissions.XpManage,
            Permissions.AnalyticsViewAll, Permissions.ReportsExport
        ],
        [RoleCodes.SupportTeacher] =
        [
            Permissions.ClassesViewAssigned, Permissions.AttendanceMark, Permissions.AttendanceView,
            Permissions.AssignmentsCreate, Permissions.AssignmentsGrade, Permissions.MessagesSend,
            Permissions.AnalyticsViewAssigned
        ],
        [RoleCodes.Student] =
        [
            Permissions.SubmissionsSubmitOwn, Permissions.MessagesSend, Permissions.AnalyticsViewOwn
        ]
    };

    public static IReadOnlyCollection<string> ForRoles(IEnumerable<string> roles)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var role in roles)
        {
            if (string.Equals(role, RoleCodes.AcademyDirector, StringComparison.OrdinalIgnoreCase))
            {
                set.UnionWith(AllPermissions);
                continue;
            }

            if (RolePermissions.TryGetValue(role, out var permissions)) set.UnionWith(permissions);
        }

        return set.ToArray();
    }
}