namespace LMS.Application.Common.Security;

/// <summary>
/// Central catalog of every permission the API enforces. Format is <c>Module.Action</c>.
///
/// Why centralize:
///   1. Single source of truth — controllers reference the constants, seed data
///      grants them by name, frontend mirrors them in `lib/auth/permissions.ts`.
///   2. The <see cref="LMS.WebApi.Security.PermissionDiscoveryHostedService"/> still
///      auto-discovers permissions from controller actions at startup, so any new
///      `[PermissionAuthorize("X.Y")]` is registered automatically — but grants in
///      <see cref="LMS.Infrastructure.Persistence.SeedData"/> must reference an
///      existing constant here.
/// </summary>
public static class Permissions
{
    public static class Auth
    {
        public const string AssignRole = "Auth.AssignRole";
    }

    public static class Users
    {
        public const string Read = "Users.Read";
        public const string Create = "Users.Create";
        public const string Update = "Users.Update";
        public const string Delete = "Users.Delete";
    }

    public static class Students
    {
        public const string Read = "Students.Read";
        public const string Create = "Students.Create";
        public const string Update = "Students.Update";
    }

    public static class Staff
    {
        public const string Read = "Staff.Read";
        public const string Create = "Staff.Create";
        public const string Update = "Staff.Update";
    }

    public static class Classes
    {
        public const string Read = "Classes.Read";
        public const string Create = "Classes.Create";
        public const string Update = "Classes.Update";
        public const string Delete = "Classes.Delete";
        public const string Enroll = "Classes.Enroll";
    }

    public static class Sessions
    {
        public const string Read = "Sessions.Read";
        public const string Create = "Sessions.Create";
        public const string Update = "Sessions.Update";
        public const string Delete = "Sessions.Delete";
    }

    public static class Assignments
    {
        public const string Read = "Assignments.Read";
        public const string Create = "Assignments.Create";
        public const string Update = "Assignments.Update";
        public const string Publish = "Assignments.Publish";
        public const string Close = "Assignments.Close";
    }

    public static class Submissions
    {
        public const string Read = "Submissions.Read";
        public const string Create = "Submissions.Create";
        public const string Grade = "Submissions.Grade";
    }

    public static class Attendance
    {
        public const string Read = "Attendance.Read";
        public const string Mark = "Attendance.Mark";
        public const string Update = "Attendance.Update";
    }

    public static class Payments
    {
        public const string Read = "Payments.Read";
        public const string Create = "Payments.Create";
        public const string Update = "Payments.Update";
    }

    public static class Badges
    {
        public const string Read = "Badges.Read";
        public const string Create = "Badges.Create";
        public const string Update = "Badges.Update";
        public const string Award = "Badges.Award";
    }

    public static class Xp
    {
        public const string Read = "Xp.Read";
        public const string Grant = "Xp.Grant";
    }

    public static class Conversations
    {
        public const string Read = "Conversations.Read";
        public const string Create = "Conversations.Create";
        public const string ManageParticipants = "Conversations.ManageParticipants";
    }

    public static class Messages
    {
        public const string Read = "Messages.Read";
        public const string Send = "Messages.Send";
    }

    public static class Courses
    {
        public const string Read = "Courses.Read";
        public const string Manage = "Courses.Manage";
    }

    public static class Rooms
    {
        public const string Read = "Rooms.Read";
        public const string Manage = "Rooms.Manage";
    }

    public static class Dashboard
    {
        public const string Director = "Dashboard.Director";
        public const string Office = "Dashboard.Office";
        public const string Teacher = "Dashboard.Teacher";
        public const string Student = "Dashboard.Student";
    }

    public static class VisitorMessages
    {
        public const string Read = "VisitorMessages.Read";
        public const string Update = "VisitorMessages.Update";
    }

    public static class Books
    {
        public const string Read = "Books.Read";
        public const string Manage = "Books.Manage";
    }

    public static class Tasks
    {
        public const string Read = "Tasks.Read";
        public const string Manage = "Tasks.Manage";
    }

    public static class TaskSubmissions
    {
        public const string Read = "TaskSubmissions.Read";
        public const string Submit = "TaskSubmissions.Submit";
        public const string Grade = "TaskSubmissions.Grade";
    }

    // Results / Roles / Permissions already existed in controllers — kept stable here.
    public static class Results
    {
        public const string Read = "Results.Read";
        public const string Create = "Results.Create";
        public const string Update = "Results.Update";
        public const string Delete = "Results.Delete";
    }

    public static class Roles
    {
        public const string Read = "Roles.Read";
        public const string Create = "Roles.Create";
        public const string Update = "Roles.Update";
        public const string Delete = "Roles.Delete";
        public const string AssignPermissions = "Roles.AssignPermissions";
    }

    public static class PermissionsCatalog
    {
        public const string Read = "Permissions.Read";
        public const string Create = "Permissions.Create";
        public const string Update = "Permissions.Update";
        public const string Delete = "Permissions.Delete";
    }

    // ─── Capability gates surfaced as sidebar items ──────────────────────────
    // These don't (yet) map to dedicated controllers, but every sidebar / route
    // gate references them so a role without the grant doesn't get an unwired
    // landing page. Granting these via the matrix below is how each role is
    // differentiated in the UI.

    /// <summary>Course materials library — readable / managed reference content.</summary>
    public static class Materials
    {
        public const string Read = "Materials.Read";
        public const string Manage = "Materials.Manage";
    }

    /// <summary>Analytics surfaces: progress, performance, class-level rollups.</summary>
    public static class Analytics
    {
        public const string Read = "Analytics.Read";
    }

    /// <summary>Operational reports (payments, attendance, enrolment).</summary>
    public static class Reports
    {
        public const string Read = "Reports.Read";
    }

    /// <summary>Self-directed practice / drills (student-facing).</summary>
    public static class Practice
    {
        public const string Read = "Practice.Read";
    }

    /// <summary>Flat list of every permission code — used by seed data and tests.</summary>
    public static IReadOnlyCollection<string> All { get; } = new[]
    {
        Auth.AssignRole,
        Users.Read, Users.Create, Users.Update, Users.Delete,
        Students.Read, Students.Create, Students.Update,
        Staff.Read, Staff.Create, Staff.Update,
        Classes.Read, Classes.Create, Classes.Update, Classes.Delete, Classes.Enroll,
        Sessions.Read, Sessions.Create, Sessions.Update, Sessions.Delete,
        Assignments.Read, Assignments.Create, Assignments.Update, Assignments.Publish, Assignments.Close,
        Submissions.Read, Submissions.Create, Submissions.Grade,
        Attendance.Read, Attendance.Mark, Attendance.Update,
        Payments.Read, Payments.Create, Payments.Update,
        Badges.Read, Badges.Create, Badges.Update, Badges.Award,
        Xp.Read, Xp.Grant,
        Conversations.Read, Conversations.Create, Conversations.ManageParticipants,
        Messages.Read, Messages.Send,
        Courses.Read, Courses.Manage,
        Rooms.Read, Rooms.Manage,
        Dashboard.Director, Dashboard.Office, Dashboard.Teacher, Dashboard.Student,
        VisitorMessages.Read, VisitorMessages.Update,
        Books.Read, Books.Manage,
        Tasks.Read, Tasks.Manage,
        TaskSubmissions.Read, TaskSubmissions.Submit, TaskSubmissions.Grade,
        Results.Read, Results.Create, Results.Update, Results.Delete,
        Roles.Read, Roles.Create, Roles.Update, Roles.Delete, Roles.AssignPermissions,
        PermissionsCatalog.Read, PermissionsCatalog.Create, PermissionsCatalog.Update, PermissionsCatalog.Delete,
        Materials.Read, Materials.Manage,
        Analytics.Read,
        Reports.Read,
        Practice.Read,
    };
}

/// <summary>
/// Default permission grants per role, applied in EF seed data. Edit here when
/// adding new permissions or rebalancing roles.
///
/// SuperAdmin and Admin are not listed — they bypass via the
/// <see cref="LMS.WebApi.Security.PermissionAuthorizationHandler"/> SuperAdmin
/// short-circuit (SuperAdmin) or by being granted <see cref="Permissions.All"/>
/// in the seeder (Admin).
/// </summary>
public static class RolePermissionMatrix
{
    public static IReadOnlyCollection<string> ForAcademyDirector { get; } = new[]
    {
        Permissions.Dashboard.Director, Permissions.Dashboard.Office,
        Permissions.Users.Read, Permissions.Users.Update,
        Permissions.Students.Read, Permissions.Students.Update,
        Permissions.Staff.Read, Permissions.Staff.Update,
        Permissions.Classes.Read, Permissions.Classes.Update,
        Permissions.Sessions.Read,
        Permissions.Assignments.Read,
        Permissions.Submissions.Read,
        Permissions.Attendance.Read,
        Permissions.Payments.Read, Permissions.Payments.Create, Permissions.Payments.Update,
        Permissions.Badges.Read, Permissions.Badges.Create,
        Permissions.Xp.Read,
        Permissions.Conversations.Read, Permissions.Conversations.Create,
        Permissions.Messages.Read, Permissions.Messages.Send,
        Permissions.Courses.Read, Permissions.Courses.Manage,
        Permissions.Rooms.Read, Permissions.Rooms.Manage,
        Permissions.Results.Read, Permissions.Results.Create, Permissions.Results.Update,
        Permissions.VisitorMessages.Read, Permissions.VisitorMessages.Update,
        Permissions.Books.Read,
        Permissions.Tasks.Read,
        Permissions.TaskSubmissions.Read,
        Permissions.Roles.Read,
        // UI capability gates — Director sees the analytics-heavy view + reports.
        Permissions.Materials.Read,
        Permissions.Analytics.Read,
        Permissions.Reports.Read,
    };

    public static IReadOnlyCollection<string> ForOfficeAdmin { get; } = new[]
    {
        Permissions.Dashboard.Office,
        Permissions.Auth.AssignRole,
        Permissions.Users.Read, Permissions.Users.Create, Permissions.Users.Update,
        Permissions.Students.Read, Permissions.Students.Create, Permissions.Students.Update,
        Permissions.Staff.Read, Permissions.Staff.Create, Permissions.Staff.Update,
        Permissions.Classes.Read, Permissions.Classes.Create, Permissions.Classes.Update,
        Permissions.Classes.Delete, Permissions.Classes.Enroll,
        Permissions.Sessions.Read, Permissions.Sessions.Create, Permissions.Sessions.Update,
        Permissions.Sessions.Delete,
        Permissions.Assignments.Read,
        Permissions.Submissions.Read,
        Permissions.Attendance.Read, Permissions.Attendance.Mark, Permissions.Attendance.Update,
        Permissions.Payments.Read, Permissions.Payments.Create, Permissions.Payments.Update,
        Permissions.Badges.Read, Permissions.Badges.Create, Permissions.Badges.Award,
        Permissions.Xp.Read, Permissions.Xp.Grant,
        Permissions.Conversations.Read, Permissions.Conversations.Create,
        Permissions.Conversations.ManageParticipants,
        Permissions.Messages.Read, Permissions.Messages.Send,
        Permissions.Courses.Read, Permissions.Courses.Manage,
        Permissions.Rooms.Read, Permissions.Rooms.Manage,
        Permissions.VisitorMessages.Read, Permissions.VisitorMessages.Update,
        Permissions.Books.Read, Permissions.Books.Manage,
        Permissions.Tasks.Read,
        Permissions.TaskSubmissions.Read,
        Permissions.Roles.Read,
        // UI capability gates — Office sees materials + ops reports, no analytics drill-down.
        Permissions.Materials.Read, Permissions.Materials.Manage,
        Permissions.Reports.Read,
    };

    public static IReadOnlyCollection<string> ForTeacher { get; } = new[]
    {
        Permissions.Dashboard.Teacher,
        Permissions.Students.Read,
        Permissions.Classes.Read,
        Permissions.Sessions.Read, Permissions.Sessions.Create, Permissions.Sessions.Update,
        Permissions.Assignments.Read, Permissions.Assignments.Create, Permissions.Assignments.Update,
        Permissions.Assignments.Publish, Permissions.Assignments.Close,
        Permissions.Submissions.Read, Permissions.Submissions.Grade,
        Permissions.Attendance.Read, Permissions.Attendance.Mark, Permissions.Attendance.Update,
        Permissions.Badges.Read, Permissions.Badges.Award,
        Permissions.Xp.Read, Permissions.Xp.Grant,
        Permissions.Conversations.Read, Permissions.Conversations.Create,
        Permissions.Messages.Read, Permissions.Messages.Send,
        Permissions.Results.Read,
        Permissions.Courses.Read,
        Permissions.Rooms.Read,
        Permissions.Books.Read,
        Permissions.Tasks.Read, Permissions.Tasks.Manage,
        Permissions.TaskSubmissions.Read, Permissions.TaskSubmissions.Grade,
        // UI capability gates — Teacher curates materials + sees class-level analytics.
        Permissions.Materials.Read, Permissions.Materials.Manage,
        Permissions.Analytics.Read,
    };

    public static IReadOnlyCollection<string> ForSupportTeacher { get; } = new[]
    {
        Permissions.Dashboard.Teacher,
        Permissions.Students.Read,
        Permissions.Classes.Read,
        Permissions.Sessions.Read,
        Permissions.Assignments.Read,
        Permissions.Submissions.Read,
        Permissions.Attendance.Read, Permissions.Attendance.Mark,
        Permissions.Conversations.Read,
        Permissions.Messages.Read, Permissions.Messages.Send,
        Permissions.Courses.Read,
        Permissions.Books.Read,
        Permissions.Tasks.Read, Permissions.Tasks.Manage,
        Permissions.TaskSubmissions.Read, Permissions.TaskSubmissions.Grade,
        // UI capability gates — Support gets read-only materials, no analytics surface.
        Permissions.Materials.Read,
    };

    public static IReadOnlyCollection<string> ForStudent { get; } = new[]
    {
        Permissions.Dashboard.Student,
        Permissions.Assignments.Read,
        Permissions.Submissions.Read, Permissions.Submissions.Create,
        Permissions.Attendance.Read,
        Permissions.Badges.Read,
        Permissions.Xp.Read,
        Permissions.Conversations.Read,
        Permissions.Messages.Read, Permissions.Messages.Send,
        Permissions.Results.Read,
        Permissions.Books.Read,
        Permissions.Tasks.Read,
        Permissions.TaskSubmissions.Submit, Permissions.TaskSubmissions.Read,
        // UI capability gates — Student gets self-directed practice + reference materials.
        Permissions.Materials.Read,
        Permissions.Practice.Read,
    };
}
