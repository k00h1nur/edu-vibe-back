using System.Text.Json;
using System.Text.Json.Nodes;
using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Exercises;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.ExerciseSets;

/// <summary>
/// Exercise-set CQRS handlers. A set is a reusable, teacher-authored collection of
/// exercises attached to classes. Exercises themselves are ordinary <see cref="LessonExercise"/>
/// rows with <c>ExerciseSetId</c> set instead of <c>LessonId</c>, so the submit / self-check /
/// XP / grading engine (SubmitExerciseAnswerCommand + ExerciseChecker) is reused verbatim —
/// there is no set-specific submit handler.
///
/// Access: management (create/update/delete/attach/author) is gated by Classes.Update on the
/// controller AND scoped here to the set's owner (or any admin). Reading a set's exercises is
/// allowed to the owner, an admin, a teacher of an attached class, or an enrolled student.
/// </summary>
public sealed class ExerciseSetsHandlers(IApplicationDbContext db, ICurrentUserService currentUser) :
    IRequestHandler<CreateExerciseSetCommand, Result<ExerciseSetDto>>,
    IRequestHandler<UpdateExerciseSetCommand, Result<ExerciseSetDto>>,
    IRequestHandler<DeleteExerciseSetCommand, Result>,
    IRequestHandler<GetExerciseSetsQuery, Result<IReadOnlyList<ExerciseSetDto>>>,
    IRequestHandler<GetExerciseSetByIdQuery, Result<ExerciseSetDto>>,
    IRequestHandler<SetExerciseSetClassesCommand, Result>,
    IRequestHandler<AddExercisesToSetCommand, Result<IReadOnlyList<Guid>>>,
    IRequestHandler<GetSetExercisesQuery, Result<IReadOnlyList<ExerciseWithResultDto>>>,
    IRequestHandler<GetStudentExerciseSetsQuery, Result<IReadOnlyList<StudentExerciseSetDto>>>
{
    public async Task<Result<ExerciseSetDto>> Handle(CreateExerciseSetCommand request, CancellationToken ct)
    {
        if (request.CreatedByUserId == Guid.Empty)
            return Result<ExerciseSetDto>.Fail("UNAUTHENTICATED", "Caller must be authenticated.");
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<ExerciseSetDto>.Fail("VALIDATION", "Title is required.");

        var set = new ExerciseSet(request.Title, request.Description, request.CreatedByUserId);
        await db.ExerciseSets.AddAsync(set, ct);
        await db.SaveChangesAsync(ct);
        return Result<ExerciseSetDto>.Ok(new ExerciseSetDto(
            set.Id, set.Title, set.Description, set.CreatedByUserId, 0, Array.Empty<Guid>(), set.CreatedAt));
    }

    public async Task<Result<ExerciseSetDto>> Handle(UpdateExerciseSetCommand request, CancellationToken ct)
    {
        var set = await db.ExerciseSets.FirstOrDefaultAsync(s => s.Id == request.SetId, ct);
        if (set is null) return Result<ExerciseSetDto>.Fail("NOT_FOUND", "Exercise set not found.");
        if (!CanManage(set)) return Result<ExerciseSetDto>.Fail("FORBIDDEN", "Not allowed to edit this set.");
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result<ExerciseSetDto>.Fail("VALIDATION", "Title is required.");

        set.SetDetails(request.Title, request.Description);
        await db.SaveChangesAsync(ct);
        return Result<ExerciseSetDto>.Ok(await MapAsync(set.Id, ct) ?? throw new InvalidOperationException());
    }

    public async Task<Result> Handle(DeleteExerciseSetCommand request, CancellationToken ct)
    {
        var set = await db.ExerciseSets.FirstOrDefaultAsync(s => s.Id == request.SetId, ct);
        if (set is null) return Result.Fail("NOT_FOUND", "Exercise set not found.");
        if (!CanManage(set)) return Result.Fail("FORBIDDEN", "Not allowed to delete this set.");

        // FK cascades take care of exercises (→ their submissions) and class links.
        db.ExerciseSets.Remove(set);
        await db.SaveChangesAsync(ct);
        return Result.Ok("Deleted");
    }

    public async Task<Result<IReadOnlyList<ExerciseSetDto>>> Handle(GetExerciseSetsQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } uid)
            return Result<IReadOnlyList<ExerciseSetDto>>.Fail("UNAUTHENTICATED", "Caller must be authenticated.");
        var isAdmin = currentUser.IsAdmin();

        var rows = await db.ExerciseSets.AsNoTracking()
            .Where(s => isAdmin || s.CreatedByUserId == uid)
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new
            {
                s.Id, s.Title, s.Description, s.CreatedByUserId, s.CreatedAt,
                ExerciseCount = db.LessonExercises.Count(e => e.ExerciseSetId == s.Id),
                ClassIds = db.ExerciseSetClasses.Where(c => c.ExerciseSetId == s.Id).Select(c => c.ClassId).ToList(),
            })
            .ToListAsync(ct);

        var items = rows.Select(s => new ExerciseSetDto(
            s.Id, s.Title, s.Description, s.CreatedByUserId, s.ExerciseCount, s.ClassIds, s.CreatedAt)).ToList();
        return Result<IReadOnlyList<ExerciseSetDto>>.Ok(items);
    }

    public async Task<Result<ExerciseSetDto>> Handle(GetExerciseSetByIdQuery request, CancellationToken ct)
    {
        var set = await db.ExerciseSets.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.SetId, ct);
        if (set is null) return Result<ExerciseSetDto>.Fail("NOT_FOUND", "Exercise set not found.");
        if (!CanManage(set)) return Result<ExerciseSetDto>.Fail("FORBIDDEN", "Not allowed to view this set.");
        return Result<ExerciseSetDto>.Ok(await MapAsync(set.Id, ct) ?? throw new InvalidOperationException());
    }

    public async Task<Result> Handle(SetExerciseSetClassesCommand request, CancellationToken ct)
    {
        var set = await db.ExerciseSets.FirstOrDefaultAsync(s => s.Id == request.SetId, ct);
        if (set is null) return Result.Fail("NOT_FOUND", "Exercise set not found.");
        if (!CanManage(set)) return Result.Fail("FORBIDDEN", "Not allowed to edit this set.");

        var wanted = request.ClassIds.Distinct().ToList();
        if (wanted.Count > 0)
        {
            var existingCount = await db.Classes.Where(c => wanted.Contains(c.Id)).CountAsync(ct);
            if (existingCount != wanted.Count)
                return Result.Fail("VALIDATION", "One or more class ids do not exist.");
        }

        // Replace the attachment set wholesale — matches how the UI sends a full re-selection.
        var current = await db.ExerciseSetClasses.Where(c => c.ExerciseSetId == set.Id).ToListAsync(ct);
        db.ExerciseSetClasses.RemoveRange(current);
        foreach (var classId in wanted)
            await db.ExerciseSetClasses.AddAsync(new ExerciseSetClass(set.Id, classId), ct);

        await db.SaveChangesAsync(ct);
        return Result.Ok("Classes updated");
    }

    public async Task<Result<IReadOnlyList<Guid>>> Handle(AddExercisesToSetCommand request, CancellationToken ct)
    {
        if (request.Exercises is null || request.Exercises.Count == 0)
            return Result<IReadOnlyList<Guid>>.Fail("VALIDATION", "At least one exercise is required.");
        var set = await db.ExerciseSets.FirstOrDefaultAsync(s => s.Id == request.SetId, ct);
        if (set is null) return Result<IReadOnlyList<Guid>>.Fail("NOT_FOUND", "Exercise set not found.");
        if (!CanManage(set)) return Result<IReadOnlyList<Guid>>.Fail("FORBIDDEN", "Not allowed to edit this set.");

        // Upsert by (ExerciseSetId, OrderIndex) — mirrors AddExercisesToLessonCommand, keyed
        // on the set owner. Exercises whose OrderIndex the client no longer sends are deleted
        // (the authoring dialog always posts the full desired set), cascading their submissions.
        return await db.ExecuteInTransactionAsync<IReadOnlyList<Guid>>(async () =>
        {
            var byOrder = (await db.LessonExercises
                    .Where(e => e.ExerciseSetId == request.SetId).ToListAsync(ct))
                .ToDictionary(e => e.OrderIndex);

            var ids = new List<Guid>(request.Exercises.Count);
            foreach (var dto in request.Exercises)
            {
                var contentJson = dto.Content.ValueKind == JsonValueKind.Undefined ? "{}" : dto.Content.GetRawText();
                if (byOrder.TryGetValue(dto.OrderIndex, out var ex))
                {
                    ex.Update(dto.Type, dto.Title ?? string.Empty, dto.OrderIndex, contentJson);
                    ids.Add(ex.Id);
                }
                else
                {
                    var created = LessonExercise.ForSet(
                        request.SetId, dto.Type, dto.Title ?? string.Empty, dto.OrderIndex, contentJson);
                    await db.LessonExercises.AddAsync(created, ct);
                    byOrder[dto.OrderIndex] = created;
                    ids.Add(created.Id);
                }
            }

            var keptOrders = request.Exercises.Select(e => e.OrderIndex).ToHashSet();
            var removed = byOrder.Values.Where(e => !keptOrders.Contains(e.OrderIndex)).ToList();
            if (removed.Count > 0) db.LessonExercises.RemoveRange(removed);

            await db.SaveChangesAsync(ct);
            return Result<IReadOnlyList<Guid>>.Ok(ids);
        }, ct);
    }

    public async Task<Result<IReadOnlyList<ExerciseWithResultDto>>> Handle(
        GetSetExercisesQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } uid)
            return Result<IReadOnlyList<ExerciseWithResultDto>>.Fail("UNAUTHENTICATED", "Caller must be authenticated.");
        if (!await db.ExerciseSets.AnyAsync(s => s.Id == request.SetId, ct))
            return Result<IReadOnlyList<ExerciseWithResultDto>>.Fail("NOT_FOUND", "Exercise set not found.");
        if (!await CanViewSetAsync(request.SetId, uid, ct))
            return Result<IReadOnlyList<ExerciseWithResultDto>>.Fail("FORBIDDEN", "Not allowed to view this set.");

        // Same shape as GetLessonExercisesQuery, filtered by the set owner.
        var rows = await db.LessonExercises.AsNoTracking()
            .Where(e => e.ExerciseSetId == request.SetId)
            .OrderBy(e => e.OrderIndex)
            .Select(e => new
            {
                e.Id, e.Type, e.Title, e.OrderIndex, e.ContentJson,
                Sub = db.LessonExerciseSubmissions
                    .Where(s => s.LessonExerciseId == e.Id && s.UserId == uid)
                    .Select(s => new
                    {
                        s.AnswersJson, s.Score, s.Total, s.IsCompleted,
                        s.TeacherScore, s.TeacherMaxScore, s.TeacherFeedback, s.GradedAt,
                    })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new ExerciseWithResultDto(
                r.Id, r.Type, r.Title, r.OrderIndex, ParseNode(r.ContentJson),
                r.Sub is null
                    ? null
                    : new ExerciseResultDto(
                        ParseNode(r.Sub.AnswersJson), r.Sub.Score, r.Sub.Total, r.Sub.IsCompleted,
                        r.Sub.TeacherScore, r.Sub.TeacherMaxScore, r.Sub.TeacherFeedback, r.Sub.GradedAt != null)))
            .ToList();
        return Result<IReadOnlyList<ExerciseWithResultDto>>.Ok(items);
    }

    public async Task<Result<IReadOnlyList<StudentExerciseSetDto>>> Handle(
        GetStudentExerciseSetsQuery request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } uid)
            return Result<IReadOnlyList<StudentExerciseSetDto>>.Fail("UNAUTHENTICATED", "Caller must be authenticated.");

        var profileId = await db.StudentProfiles
            .Where(p => p.UserId == uid).Select(p => (Guid?)p.Id).FirstOrDefaultAsync(ct);
        if (profileId is null)
            return Result<IReadOnlyList<StudentExerciseSetDto>>.Ok(Array.Empty<StudentExerciseSetDto>());

        // Reachability: my active enrolments → their classes → sets attached to any of them.
        var classIds = db.Enrollments
            .Where(e => e.StudentProfileId == profileId && e.Status != EnrollmentStatus.Dropped)
            .Select(e => e.ClassId);
        var setIds = db.ExerciseSetClasses.Where(c => classIds.Contains(c.ClassId)).Select(c => c.ExerciseSetId);

        var items = await db.ExerciseSets.AsNoTracking()
            .Where(s => setIds.Contains(s.Id))
            .OrderByDescending(s => s.CreatedAt)
            .Select(s => new StudentExerciseSetDto(
                s.Id, s.Title, s.Description,
                db.LessonExercises.Count(e => e.ExerciseSetId == s.Id),
                db.LessonExercises.Count(e => e.ExerciseSetId == s.Id
                    && db.LessonExerciseSubmissions.Any(sub =>
                        sub.LessonExerciseId == e.Id && sub.UserId == uid && sub.IsCompleted))))
            .ToListAsync(ct);
        return Result<IReadOnlyList<StudentExerciseSetDto>>.Ok(items);
    }

    // ---- helpers ----------------------------------------------------------

    private bool CanManage(ExerciseSet set) =>
        currentUser.IsAdmin() || (currentUser.UserId is { } uid && set.CreatedByUserId == uid);

    /// <summary>Owner, admin, a teacher of an attached class, or an enrolled student may view.</summary>
    private async Task<bool> CanViewSetAsync(Guid setId, Guid uid, CancellationToken ct)
    {
        if (currentUser.IsAdmin()) return true;
        if (await db.ExerciseSets.AnyAsync(s => s.Id == setId && s.CreatedByUserId == uid, ct)) return true;

        var teaches = await db.ExerciseSetClasses
            .Where(esc => esc.ExerciseSetId == setId)
            .AnyAsync(esc => db.Classes.Any(c => c.Id == esc.ClassId && c.TeacherUserId == uid), ct);
        if (teaches) return true;

        return await db.ExerciseSetClasses
            .Where(esc => esc.ExerciseSetId == setId)
            .AnyAsync(esc => db.Enrollments.Any(e =>
                e.ClassId == esc.ClassId && e.Status != EnrollmentStatus.Dropped
                && db.StudentProfiles.Any(sp => sp.UserId == uid && sp.Id == e.StudentProfileId)), ct);
    }

    private async Task<ExerciseSetDto?> MapAsync(Guid setId, CancellationToken ct)
    {
        return await db.ExerciseSets.AsNoTracking()
            .Where(s => s.Id == setId)
            .Select(s => new ExerciseSetDto(
                s.Id, s.Title, s.Description, s.CreatedByUserId,
                db.LessonExercises.Count(e => e.ExerciseSetId == s.Id),
                db.ExerciseSetClasses.Where(c => c.ExerciseSetId == s.Id).Select(c => c.ClassId).ToList(),
                s.CreatedAt))
            .FirstOrDefaultAsync(ct);
    }

    private static JsonNode? ParseNode(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try { return JsonNode.Parse(json); }
        catch { return null; }
    }
}
