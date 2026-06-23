using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Curriculum;

// ---- Course Builder DTOs --------------------------------------------------

/// <summary>A unit as shown on the teacher's roadmap card (counts + its lessons).</summary>
public sealed record CourseBuilderUnitDto(
    Guid Id, int Order, string Title, string? Description,
    int LessonCount, int MaterialCount, int HomeworkCount, int AssessmentCount,
    IReadOnlyList<CurriculumLessonDto> Lessons);

/// <summary>
/// A class's editable course structure. Every mutation returns the whole tree so
/// the client re-renders the roadmap from a single response (no client-side merge).
/// </summary>
public sealed record ClassCourseBuilderDto(
    Guid ClassId, Guid TemplateId, string TemplateName, IReadOnlyList<CourseBuilderUnitDto> Units);

// ---- Queries / Commands (all self-scoped to the class teacher / admin) -----

/// <summary>Reads (and lazily provisions) the class's own editable course structure.</summary>
public sealed record GetClassCourseBuilderQuery(Guid ClassId) : IRequest<Result<ClassCourseBuilderDto>>;

public sealed record CreateCourseUnitCommand(Guid ClassId, string Title, string? Description)
    : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record UpdateCourseUnitCommand(Guid UnitId, string Title, string? Description)
    : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record DeleteCourseUnitCommand(Guid UnitId) : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record ReorderCourseUnitsCommand(Guid ClassId, IReadOnlyList<Guid> UnitIds)
    : IRequest<Result<ClassCourseBuilderDto>>;

public sealed record CreateCourseLessonCommand(
    Guid UnitId, string Title, string? Objectives, string? Homework, string? Materials, bool IsAssessment)
    : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record UpdateCourseLessonCommand(
    Guid LessonId, string Title, string? Objectives, string? Homework, string? Materials, bool IsAssessment)
    : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record DeleteCourseLessonCommand(Guid LessonId) : IRequest<Result<ClassCourseBuilderDto>>;
public sealed record ReorderCourseLessonsCommand(Guid UnitId, IReadOnlyList<Guid> LessonIds)
    : IRequest<Result<ClassCourseBuilderDto>>;
