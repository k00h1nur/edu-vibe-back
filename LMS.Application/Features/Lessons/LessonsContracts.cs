using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Lessons;

// ---- DTOs -----------------------------------------------------------------

public sealed record LessonMaterialDto(
    Guid Id,
    Guid ClassSessionId,
    string FileName,
    string FileType,
    long FileSize,
    DateTime CreatedAt);

/// <summary>An assignment surfaced on a lesson hub, flagged if linked to it.</summary>
public sealed record LessonAssignmentDto(
    Guid Id,
    string Title,
    AssignmentStatus Status,
    DateTime? DueDate,
    bool LinkedToThisLesson);

/// <summary>
/// The whole lesson hub in one payload: session + online details + video +
/// materials + assignments + the caller's progress, plus a CanManage flag so
/// the UI knows whether to render teacher controls.
/// </summary>
public sealed record LessonFullDto(
    Guid Id,
    Guid ClassId,
    string ClassTitle,
    Guid? TeacherUserId,
    DateOnly SessionDate,
    TimeOnly StartsAt,
    TimeOnly EndsAt,
    Guid? RoomId,
    string? Topic,
    string? MeetingUrl,
    string? Notes,
    string? VideoUrl,
    bool CanManage,
    bool Completed,
    DateTime? CompletedAt,
    IReadOnlyCollection<LessonMaterialDto> Materials,
    IReadOnlyCollection<LessonAssignmentDto> Assignments);

public sealed record LessonMaterialDownloadDto(string StoredFileName, string OriginalFileName, string MimeType);

// ---- Queries / Commands ---------------------------------------------------

/// <summary>The full lesson hub. Readable by the class teacher, an enrolled student, or staff.</summary>
public sealed record GetLessonFullQuery(Guid SessionId) : IRequest<Result<LessonFullDto>>;

/// <summary>Teacher attaches an already-stored file blob to a lesson (self-scoped to the class teacher).</summary>
public sealed record AddLessonMaterialCommand(
    Guid SessionId,
    string StoredFileName,
    string OriginalFileName,
    string MimeType,
    long FileSize) : IRequest<Result<LessonMaterialDto>>;

/// <summary>Removes a lesson material. Returns the stored file name so the controller deletes the blob.</summary>
public sealed record RemoveLessonMaterialCommand(Guid SessionId, Guid MaterialId) : IRequest<Result<string>>;

/// <summary>Sets/clears the lesson's video URL (teacher self-scoped).</summary>
public sealed record SetLessonVideoCommand(Guid SessionId, string? VideoUrl) : IRequest<Result>;

/// <summary>Student marks the lesson complete (true) or clears it (false). Self-scoped to the student.</summary>
public sealed record SetLessonProgressCommand(Guid SessionId, bool Completed) : IRequest<Result>;

/// <summary>Teacher links/unlinks one of the class's assignments to this lesson.</summary>
public sealed record LinkLessonAssignmentCommand(Guid SessionId, Guid AssignmentId, bool Linked) : IRequest<Result>;

/// <summary>Resolves a material for download after the per-call access check.</summary>
public sealed record GetLessonMaterialForDownloadQuery(Guid MaterialId) : IRequest<Result<LessonMaterialDownloadDto>>;
