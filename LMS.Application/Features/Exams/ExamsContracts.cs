using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Exams;

// ---- read DTOs -------------------------------------------------------------

public sealed record ExamSectionDto(Guid Id, string Name, decimal MaxScore, int Order);

public sealed record ExamDto(
    Guid Id,
    Guid ClassId,
    Guid CurriculumLessonId,
    string Title,
    decimal? PassThresholdPercent,
    decimal EffectiveThresholdPercent,
    IReadOnlyCollection<ExamSectionDto> Sections);

public sealed record ExamSectionScoreDto(Guid ExamSectionId, decimal Score);

public sealed record ExamResultDto(
    Guid Id,
    Guid ExamId,
    Guid StudentProfileId,
    decimal OverallPercent,
    bool Passed,
    DateTime EnteredAt,
    IReadOnlyCollection<ExamSectionScoreDto> SectionScores);

/// <summary>A roster row for the score-entry grid: a student + their current result (null = not yet entered).</summary>
public sealed record ExamRosterRowDto(
    Guid StudentProfileId,
    string? FirstName,
    string? LastName,
    string Email,
    ExamResultDto? Result);

public sealed record ExamRosterDto(ExamDto Exam, IReadOnlyCollection<ExamRosterRowDto> Rows);

/// <summary>Per-section breakdown shown on the student profile.</summary>
public sealed record StudentExamSectionDto(string Name, decimal Score, decimal MaxScore);

public sealed record StudentExamResultDto(
    Guid ExamId,
    string ExamTitle,
    Guid ClassId,
    string? ClassTitle,
    decimal OverallPercent,
    bool Passed,
    decimal ThresholdPercent,
    DateTime EnteredAt,
    IReadOnlyCollection<StudentExamSectionDto> Sections);

// ---- write DTOs ------------------------------------------------------------

/// <summary>A requested section in a create/update. Id null = new section.</summary>
public sealed record ExamSectionInputDto(Guid? Id, string Name, decimal MaxScore, int Order);

public sealed record SectionScoreInputDto(Guid ExamSectionId, decimal Score);

// ---- commands / queries ----------------------------------------------------

public sealed record CreateExamCommand(
    Guid ClassId,
    Guid CurriculumLessonId,
    string Title,
    decimal? PassThresholdPercent,
    IReadOnlyCollection<ExamSectionInputDto> Sections) : IRequest<Result<ExamDto>>;

public sealed record UpdateExamCommand(
    Guid ExamId,
    string Title,
    decimal? PassThresholdPercent,
    IReadOnlyCollection<ExamSectionInputDto> Sections) : IRequest<Result<ExamDto>>;

public sealed record DeleteExamCommand(Guid ExamId) : IRequest<Result>;

public sealed record GetExamByIdQuery(Guid ExamId) : IRequest<Result<ExamDto>>;

public sealed record GetClassExamsQuery(Guid ClassId) : IRequest<Result<IReadOnlyCollection<ExamDto>>>;

public sealed record GetExamRosterQuery(Guid ExamId) : IRequest<Result<ExamRosterDto>>;

/// <summary>Enter/correct one student's per-section scores (idempotent upsert + recompute).</summary>
public sealed record EnterExamResultCommand(
    Guid ExamId,
    Guid StudentProfileId,
    IReadOnlyCollection<SectionScoreInputDto> Scores) : IRequest<Result<ExamResultDto>>;

public sealed record DeleteExamResultCommand(Guid ExamId, Guid StudentProfileId) : IRequest<Result>;

public sealed record GetStudentExamResultsQuery(Guid StudentProfileId)
    : IRequest<Result<IReadOnlyCollection<StudentExamResultDto>>>;
