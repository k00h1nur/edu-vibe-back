using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;

namespace LMS.Application.Features.Curriculum;

// ---- DTOs -----------------------------------------------------------------

public sealed record CurriculumTemplateSummaryDto(
    Guid Id, string Name, CurriculumCategory Category, string? Level, string? Description,
    bool IsSystem, int ModuleCount, int UnitCount, int LessonCount);

public sealed record CurriculumLessonDto(
    Guid Id, int Order, string Title, string? Objectives,
    string? HomeworkPlaceholder, string? MaterialsPlaceholder, bool IsAssessment);

public sealed record CurriculumUnitDto(Guid Id, int Order, string Title, IReadOnlyList<CurriculumLessonDto> Lessons);

public sealed record CurriculumModuleDto(Guid Id, int Order, string Title, IReadOnlyList<CurriculumUnitDto> Units);

public sealed record CurriculumTreeDto(
    Guid Id, string Name, CurriculumCategory Category, string? Level, string? Description,
    bool IsSystem, IReadOnlyList<CurriculumModuleDto> Modules);

/// <summary>A scheduled session annotated with its curriculum lesson — the planner row.</summary>
public sealed record ScheduledLessonDto(
    Guid SessionId, DateOnly Date, TimeOnly StartsAt, TimeOnly EndsAt,
    string? ModuleTitle, string? UnitTitle, string Topic, string? Objectives,
    Guid? CurriculumLessonId, bool IsAssessment, bool IsPast);

/// <summary>The class's curriculum view: progress + today/next + the full dated plan (weekly/monthly/full-course source).</summary>
public sealed record ClassCurriculumDto(
    Guid ClassId, Guid? TemplateId, string? TemplateName,
    int TotalLessons, int CompletedLessons,
    ScheduledLessonDto? Today, ScheduledLessonDto? Next,
    IReadOnlyList<ScheduledLessonDto> Schedule);

// ---- Queries / Commands ---------------------------------------------------

public sealed record GetCurriculumTemplatesQuery(CurriculumCategory? Category = null)
    : IRequest<Result<IReadOnlyCollection<CurriculumTemplateSummaryDto>>>;

public sealed record GetCurriculumTreeQuery(Guid TemplateId) : IRequest<Result<CurriculumTreeDto>>;

/// <summary>
/// Binds a class to a template and maps the class's UPCOMING sessions (date
/// order) to the template's lessons (module→unit→lesson order), copying each
/// lesson's topic onto the session. Integrates with the existing schedule —
/// it annotates sessions ApplyClassSchedule already generated, never a parallel
/// system. Idempotent: re-running re-maps from today forward.
/// </summary>
public sealed record AssignCurriculumToClassCommand(Guid ClassId, Guid TemplateId)
    : IRequest<Result<ClassCurriculumDto>>;

public sealed record GetClassCurriculumQuery(Guid ClassId) : IRequest<Result<ClassCurriculumDto>>;
