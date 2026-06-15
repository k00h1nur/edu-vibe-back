using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Analytics;

// ---- Student performance --------------------------------------------------

/// <summary>
/// A student's measurable learning summary, aggregated over existing data
/// (attendance, submissions, task submissions, lesson progress). Percentages
/// are 0–100; AverageScore is 0–100 across graded assignments + quizzes, null
/// when nothing is graded yet.
/// </summary>
public sealed record StudentPerformanceDto(
    Guid StudentProfileId,
    double AttendancePercent,
    double AssignmentCompletionPercent,
    double LessonCompletionPercent,
    double? AverageScore,
    int MissingAssignments,
    int LateSubmissions,
    int AttendanceMarks,
    int AttendedSessions,
    int PublishedAssignments,
    int SubmittedAssignments,
    int TotalLessons,
    int CompletedLessons);

/// <summary>Readable by the student themselves, a teacher of one of their classes, or staff.</summary>
public sealed record GetStudentPerformanceQuery(Guid StudentProfileId) : IRequest<Result<StudentPerformanceDto>>;

// ---- Class analytics ------------------------------------------------------

public sealed record AtRiskStudentDto(
    Guid StudentProfileId,
    string Name,
    double AttendancePercent,
    double AssignmentCompletionPercent,
    double? AverageScore,
    IReadOnlyCollection<string> Reasons);

/// <summary>
/// Class-level engagement + outcomes. Rates are 0–100. At-risk rule (any of):
/// attendance &lt; 70, assignment completion &lt; 50, average grade &lt; 60.
/// </summary>
public sealed record ClassAnalyticsDto(
    Guid ClassId,
    string ClassTitle,
    int StudentCount,
    double AttendanceRate,
    double? AverageGrade,
    double AssignmentCompletionRate,
    double LessonCompletionRate,
    int PublishedAssignments,
    int TotalSessions,
    IReadOnlyCollection<AtRiskStudentDto> AtRisk);

/// <summary>Readable by the class's own teacher or staff.</summary>
public sealed record GetClassAnalyticsQuery(Guid ClassId) : IRequest<Result<ClassAnalyticsDto>>;

// ---- Attendance: bulk + session summary -----------------------------------

public sealed record AttendanceMarkInput(Guid StudentProfileId, AttendanceStatus Status);

/// <summary>
/// One row of a session's attendance: the student + their current status
/// (null = not marked yet).
/// </summary>
public sealed record SessionAttendanceRowDto(Guid StudentProfileId, string Name, AttendanceStatus? Status);

/// <summary>Session attendance roster + status counts.</summary>
public sealed record SessionAttendanceDto(
    Guid SessionId,
    Guid ClassId,
    int Present,
    int Absent,
    int Late,
    int Excused,
    int Unmarked,
    int Total,
    IReadOnlyCollection<SessionAttendanceRowDto> Rows);

/// <summary>Bulk-upsert attendance for a session. Teacher (own class) or staff only.</summary>
public sealed record BulkMarkAttendanceCommand(Guid SessionId, IReadOnlyCollection<AttendanceMarkInput> Marks)
    : IRequest<Result<SessionAttendanceDto>>;

/// <summary>Session roster + each student's status + summary counts. Teacher (own class) or staff.</summary>
public sealed record GetSessionAttendanceSummaryQuery(Guid SessionId) : IRequest<Result<SessionAttendanceDto>>;
