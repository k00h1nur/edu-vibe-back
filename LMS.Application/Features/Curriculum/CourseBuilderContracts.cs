using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;

namespace LMS.Application.Features.Curriculum;

// ---- Course Builder DTOs --------------------------------------------------

/// <summary>A lesson as shown inside a unit on the builder — rich fields included.</summary>
public sealed record CourseBuilderLessonDto(
    Guid Id, int Order, string Title, string? Objectives,
    string? HomeworkPlaceholder, string? MaterialsPlaceholder, bool IsAssessment,
    CurriculumLessonType LessonType, int? DurationMinutes, int XpReward, int ExerciseCount,
    bool InPlan);

/// <summary>A unit as shown on the teacher's roadmap card (meta + counts + its lessons).</summary>
public sealed record CourseBuilderUnitDto(
    Guid Id, int Order, string Title, string? Description,
    string? Icon, int? EstimatedMinutes, int XpReward,
    int LessonCount, int MaterialCount, int HomeworkCount, int AssessmentCount, int TotalXp,
    IReadOnlyList<CourseBuilderLessonDto> Lessons);

/// <summary>
/// A class's editable course structure. Every mutation returns the whole tree so
/// the client re-renders the roadmap from a single response (no client-side merge).
/// </summary>
public sealed record ClassCourseBuilderDto(
    Guid ClassId, Guid TemplateId, string TemplateName, IReadOnlyList<CourseBuilderUnitDto> Units);

// ---- Queries / Commands (all self-scoped to the class teacher / admin) -----

/// <summary>Reads (and lazily provisions) the class's own editable course structure.</summary>
public sealed record GetClassCourseBuilderQuery(Guid ClassId) : IRequest<Result<ClassCourseBuilderDto>>;

/// <summary>
/// Deep-copies a curriculum template (its modules → units → lessons, with all
/// rich fields) into a fresh editable course owned by the class, and points the
/// class at it. The one-click "start from a template" flow — no manual unit
/// creation. Returns the whole built course so the roadmap renders immediately.
/// </summary>
public sealed record CloneTemplateToClassCommand(Guid ClassId, Guid TemplateId)
    : IRequest<Result<ClassCourseBuilderDto>>;

// New optional fields default so existing callers (title+description only) keep working.
public sealed record CreateCourseUnitCommand(
    Guid ClassId, string Title, string? Description,
    string? Icon = null, int? EstimatedMinutes = null, int XpReward = 0)
    : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record UpdateCourseUnitCommand(
    Guid UnitId, string Title, string? Description,
    string? Icon = null, int? EstimatedMinutes = null, int XpReward = 0)
    : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record DeleteCourseUnitCommand(Guid UnitId) : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record DuplicateCourseUnitCommand(Guid UnitId) : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record ReorderCourseUnitsCommand(Guid ClassId, IReadOnlyList<Guid> UnitIds)
    : IRequest<Result<ClassCourseBuilderDto>>;

/// <summary>One lesson inside a bulk unit-create payload.</summary>
public sealed record BulkLessonInput(
    string Title, string? Objectives, string? Homework, string? Materials, bool IsAssessment,
    CurriculumLessonType LessonType = CurriculumLessonType.General, int? DurationMinutes = null, int XpReward = 0);

/// <summary>
/// Create a whole unit and all its lessons in one transaction — the "one-click"
/// builder flow. Lessons are created in array order.
/// </summary>
public sealed record BulkCreateUnitCommand(
    Guid ClassId, string Title, string? Description,
    string? Icon, int? EstimatedMinutes, int XpReward,
    IReadOnlyList<BulkLessonInput> Lessons) : IRequest<Result<ClassCourseBuilderDto>>;

public sealed record CreateCourseLessonCommand(
    Guid UnitId, string Title, string? Objectives, string? Homework, string? Materials, bool IsAssessment,
    CurriculumLessonType LessonType = CurriculumLessonType.General, int? DurationMinutes = null, int XpReward = 0)
    : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record UpdateCourseLessonCommand(
    Guid LessonId, string Title, string? Objectives, string? Homework, string? Materials, bool IsAssessment,
    CurriculumLessonType LessonType = CurriculumLessonType.General, int? DurationMinutes = null, int XpReward = 0)
    : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record DeleteCourseLessonCommand(Guid LessonId) : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record DuplicateCourseLessonCommand(Guid LessonId) : IRequest<Result<ClassCourseBuilderDto>>;
/// <summary>Move a lesson to another unit in the same course (optionally at a given 1-based slot).</summary>
public sealed record MoveCourseLessonCommand(Guid LessonId, Guid TargetUnitId, int? TargetOrder = null)
    : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record ReorderCourseLessonsCommand(Guid UnitId, IReadOnlyList<Guid> LessonIds)
    : IRequest<Result<ClassCourseBuilderDto>>;
