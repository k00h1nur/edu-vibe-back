using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Lessons;

/// <summary>
/// Lesson hub backend. Every handler self-scopes to the caller's relationship
/// with the session's class:
///   • teacher of the class  → full read + manage
///   • enrolled student      → read + own progress
///   • staff (admin/office)  → read
/// No cross-class access (the access check runs per call), so these endpoints
/// need no broad Sessions/Materials permission.
/// </summary>
public sealed class LessonsHandlers(IApplicationDbContext db, ICurrentUserService currentUser) :
    IRequestHandler<GetLessonFullQuery, Result<LessonFullDto>>,
    IRequestHandler<AddLessonMaterialCommand, Result<LessonMaterialDto>>,
    IRequestHandler<RemoveLessonMaterialCommand, Result<string>>,
    IRequestHandler<SetLessonVideoCommand, Result>,
    IRequestHandler<SetLessonProgressCommand, Result>,
    IRequestHandler<LinkLessonAssignmentCommand, Result>,
    IRequestHandler<GetLessonMaterialForDownloadQuery, Result<LessonMaterialDownloadDto>>
{
    private sealed record Access(Guid ClassId, string ClassTitle, Guid? TeacherUserId,
        bool IsTeacher, bool IsEnrolledStudent, bool IsStaff)
    {
        public bool CanRead => IsTeacher || IsEnrolledStudent || IsStaff;
    }

    /// <summary>Loads the session's class + the caller's relationship to it. Null = session not found.</summary>
    private async Task<Access?> ResolveAsync(Guid sessionId, CancellationToken ct)
    {
        var info = await db.ClassSessions.AsNoTracking()
            .Where(s => s.Id == sessionId)
            .Join(db.Classes, s => s.ClassId, c => c.Id,
                (s, c) => new { c.Id, c.Title, c.TeacherUserId })
            .FirstOrDefaultAsync(ct);
        if (info is null) return null;

        var uid = currentUser.UserId;
        var isTeacher = uid is not null && info.TeacherUserId == uid;
        var isStaff = currentUser.StaffProfileId is not null && !isTeacher;
        var spId = currentUser.StudentProfileId;
        var isEnrolled = spId is not null && await db.Enrollments.AsNoTracking()
            .AnyAsync(e => e.ClassId == info.Id && e.StudentProfileId == spId && e.Status != EnrollmentStatus.Dropped, ct);

        return new Access(info.Id, info.Title, info.TeacherUserId, isTeacher, isEnrolled, isStaff);
    }

    private static string FileTypeOf(string fileName)
    {
        var ext = System.IO.Path.GetExtension(fileName);
        return string.IsNullOrEmpty(ext) ? "" : ext.TrimStart('.').ToLowerInvariant();
    }

    public async Task<Result<LessonFullDto>> Handle(GetLessonFullQuery request, CancellationToken ct)
    {
        var access = await ResolveAsync(request.SessionId, ct);
        if (access is null) return Result<LessonFullDto>.Fail("NOT_FOUND", "Lesson not found.");
        if (!access.CanRead) return Result<LessonFullDto>.Fail("FORBIDDEN", "You don't have access to this lesson.");

        var s = await db.ClassSessions.AsNoTracking().FirstAsync(x => x.Id == request.SessionId, ct);

        var rawMaterials = await db.LessonMaterials.AsNoTracking()
            .Where(m => m.ClassSessionId == request.SessionId)
            .OrderBy(m => m.CreatedAt)
            .Select(m => new { m.Id, m.ClassSessionId, m.OriginalFileName, m.FileSize, m.CreatedAt })
            .ToListAsync(ct);
        var materials = rawMaterials
            .Select(m => new LessonMaterialDto(m.Id, m.ClassSessionId, m.OriginalFileName,
                FileTypeOf(m.OriginalFileName), m.FileSize, m.CreatedAt))
            .ToList();

        // Class assignments; students see only published. Linked-to-this-lesson first.
        var assignmentsQuery = db.Assignments.AsNoTracking().Where(a => a.ClassId == access.ClassId);
        if (!access.IsTeacher && !access.IsStaff)
            assignmentsQuery = assignmentsQuery.Where(a => a.Status == AssignmentStatus.Published);
        var assignments = await assignmentsQuery
            .OrderByDescending(a => a.ClassSessionId == request.SessionId)
            .ThenBy(a => a.Title)
            .Select(a => new LessonAssignmentDto(a.Id, a.Title, a.Status, a.DueDate,
                a.ClassSessionId == request.SessionId))
            .ToListAsync(ct);

        // Progress for the calling student (if any).
        DateTime? completedAt = null;
        var spId = currentUser.StudentProfileId;
        if (spId is not null)
        {
            completedAt = await db.LessonProgress.AsNoTracking()
                .Where(p => p.ClassSessionId == request.SessionId && p.StudentProfileId == spId)
                .Select(p => (DateTime?)p.CompletedAt)
                .FirstOrDefaultAsync(ct);
        }

        var dto = new LessonFullDto(
            s.Id, access.ClassId, access.ClassTitle, access.TeacherUserId,
            s.SessionDate, s.StartsAt, s.EndsAt, s.RoomId,
            s.Topic, s.MeetingUrl, s.Notes, s.VideoUrl,
            access.IsTeacher, completedAt is not null, completedAt,
            materials, assignments);
        return Result<LessonFullDto>.Ok(dto);
    }

    public async Task<Result<LessonMaterialDto>> Handle(AddLessonMaterialCommand request, CancellationToken ct)
    {
        var access = await ResolveAsync(request.SessionId, ct);
        if (access is null) return Result<LessonMaterialDto>.Fail("NOT_FOUND", "Lesson not found.");
        if (!access.IsTeacher) return Result<LessonMaterialDto>.Fail("FORBIDDEN", "Only the class teacher can add materials.");

        var m = new LessonMaterial(request.SessionId, request.StoredFileName, request.OriginalFileName,
            request.MimeType, request.FileSize, currentUser.UserId!.Value);
        await db.LessonMaterials.AddAsync(m, ct);
        await db.SaveChangesAsync(ct);
        return Result<LessonMaterialDto>.Ok(
            new LessonMaterialDto(m.Id, m.ClassSessionId, m.OriginalFileName,
                FileTypeOf(m.OriginalFileName), m.FileSize, m.CreatedAt), "Material added.");
    }

    public async Task<Result<string>> Handle(RemoveLessonMaterialCommand request, CancellationToken ct)
    {
        var access = await ResolveAsync(request.SessionId, ct);
        if (access is null) return Result<string>.Fail("NOT_FOUND", "Lesson not found.");
        if (!access.IsTeacher) return Result<string>.Fail("FORBIDDEN", "Only the class teacher can remove materials.");

        var m = await db.LessonMaterials
            .FirstOrDefaultAsync(x => x.Id == request.MaterialId && x.ClassSessionId == request.SessionId, ct);
        if (m is null) return Result<string>.Fail("NOT_FOUND", "Material not found.");
        var stored = m.StoredFileName;
        db.LessonMaterials.Remove(m);
        await db.SaveChangesAsync(ct);
        return Result<string>.Ok(stored, "Material removed.");
    }

    public async Task<Result> Handle(SetLessonVideoCommand request, CancellationToken ct)
    {
        var access = await ResolveAsync(request.SessionId, ct);
        if (access is null) return Result.Fail("NOT_FOUND", "Lesson not found.");
        if (!access.IsTeacher) return Result.Fail("FORBIDDEN", "Only the class teacher can set the video.");

        var s = await db.ClassSessions.FirstAsync(x => x.Id == request.SessionId, ct);
        s.SetVideo(request.VideoUrl);
        await db.SaveChangesAsync(ct);
        return Result.Ok("Video updated.");
    }

    public async Task<Result> Handle(SetLessonProgressCommand request, CancellationToken ct)
    {
        var access = await ResolveAsync(request.SessionId, ct);
        if (access is null) return Result.Fail("NOT_FOUND", "Lesson not found.");
        var spId = currentUser.StudentProfileId;
        if (spId is null || !access.IsEnrolledStudent)
            return Result.Fail("FORBIDDEN", "Only an enrolled student can track lesson progress.");

        var existing = await db.LessonProgress
            .FirstOrDefaultAsync(p => p.ClassSessionId == request.SessionId && p.StudentProfileId == spId, ct);
        if (request.Completed)
        {
            if (existing is null)
            {
                await db.LessonProgress.AddAsync(new LessonProgress(spId.Value, request.SessionId), ct);
                await db.SaveChangesAsync(ct);
            }
            return Result.Ok("Lesson marked complete.");
        }
        if (existing is not null)
        {
            db.LessonProgress.Remove(existing);
            await db.SaveChangesAsync(ct);
        }
        return Result.Ok("Lesson marked incomplete.");
    }

    public async Task<Result> Handle(LinkLessonAssignmentCommand request, CancellationToken ct)
    {
        var access = await ResolveAsync(request.SessionId, ct);
        if (access is null) return Result.Fail("NOT_FOUND", "Lesson not found.");
        if (!access.IsTeacher) return Result.Fail("FORBIDDEN", "Only the class teacher can link assignments.");

        var a = await db.Assignments.FirstOrDefaultAsync(x => x.Id == request.AssignmentId, ct);
        if (a is null) return Result.Fail("NOT_FOUND", "Assignment not found.");
        if (a.ClassId != access.ClassId)
            return Result.Fail("VALIDATION", "Assignment belongs to a different class.");

        a.SetSession(request.Linked ? request.SessionId : null);
        await db.SaveChangesAsync(ct);
        return Result.Ok(request.Linked ? "Assignment linked." : "Assignment unlinked.");
    }

    public async Task<Result<LessonMaterialDownloadDto>> Handle(GetLessonMaterialForDownloadQuery request,
        CancellationToken ct)
    {
        var m = await db.LessonMaterials.AsNoTracking()
            .Where(x => x.Id == request.MaterialId)
            .Select(x => new { x.ClassSessionId, x.StoredFileName, x.OriginalFileName, x.MimeType })
            .FirstOrDefaultAsync(ct);
        if (m is null) return Result<LessonMaterialDownloadDto>.Fail("NOT_FOUND", "Material not found.");

        var access = await ResolveAsync(m.ClassSessionId, ct);
        if (access is null || !access.CanRead)
            return Result<LessonMaterialDownloadDto>.Fail("FORBIDDEN", "You don't have access to this material.");

        return Result<LessonMaterialDownloadDto>.Ok(
            new LessonMaterialDownloadDto(m.StoredFileName, m.OriginalFileName, m.MimeType));
    }
}
