using System.Text.Json;
using System.Text.Json.Nodes;
using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Exercises;

public sealed class ExercisesHandlers(IApplicationDbContext db) :
    IRequestHandler<AddExercisesToLessonCommand, Result<IReadOnlyList<Guid>>>,
    IRequestHandler<GetLessonExercisesQuery, Result<IReadOnlyList<ExerciseWithResultDto>>>,
    IRequestHandler<SubmitExerciseAnswerCommand, Result<SubmitResultDto>>,
    IRequestHandler<GetLessonExerciseResultsQuery, Result<LessonExerciseResultsDto>>
{
    public async Task<Result<IReadOnlyList<Guid>>> Handle(AddExercisesToLessonCommand request, CancellationToken ct)
    {
        if (request.Exercises is null || request.Exercises.Count == 0)
            return Result<IReadOnlyList<Guid>>.Fail("VALIDATION", "At least one exercise is required.");
        if (!await db.CurriculumLessons.AnyAsync(l => l.Id == request.LessonId, ct))
            return Result<IReadOnlyList<Guid>>.Fail("NOT_FOUND", "Lesson not found.");

        // Upsert by (LessonId, OrderIndex), atomically (retriable transaction).
        return await db.ExecuteInTransactionAsync<IReadOnlyList<Guid>>(async () =>
        {
            var byOrder = (await db.LessonExercises
                    .Where(e => e.LessonId == request.LessonId).ToListAsync(ct))
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
                    var created = new LessonExercise(
                        request.LessonId, dto.Type, dto.Title ?? string.Empty, dto.OrderIndex, contentJson);
                    await db.LessonExercises.AddAsync(created, ct);
                    byOrder[dto.OrderIndex] = created;
                    ids.Add(created.Id);
                }
            }

            // Sync: drop exercises the client no longer includes (removed in the editor).
            // The authoring dialog always sends the FULL desired set, so this only deletes
            // what the teacher explicitly removed. Cascades that exercise's self-check
            // submissions — intentional, since the lesson's exercise set is being redefined.
            var keptOrders = request.Exercises.Select(e => e.OrderIndex).ToHashSet();
            var removed = byOrder.Values.Where(e => !keptOrders.Contains(e.OrderIndex)).ToList();
            if (removed.Count > 0) db.LessonExercises.RemoveRange(removed);

            await db.SaveChangesAsync(ct);
            return Result<IReadOnlyList<Guid>>.Ok(ids);
        }, ct);
    }

    public async Task<Result<IReadOnlyList<ExerciseWithResultDto>>> Handle(
        GetLessonExercisesQuery request, CancellationToken ct)
    {
        // Single query: each exercise with the user's result via a correlated LEFT JOIN.
        var rows = await db.LessonExercises.AsNoTracking()
            .Where(e => e.LessonId == request.LessonId)
            .OrderBy(e => e.OrderIndex)
            .Select(e => new
            {
                e.Id,
                e.Type,
                e.Title,
                e.OrderIndex,
                e.ContentJson,
                Sub = db.LessonExerciseSubmissions
                    .Where(s => s.LessonExerciseId == e.Id && s.UserId == request.UserId)
                    .Select(s => new { s.AnswersJson, s.Score, s.Total, s.IsCompleted })
                    .FirstOrDefault(),
            })
            .ToListAsync(ct);

        var items = rows.Select(r => new ExerciseWithResultDto(
                r.Id, r.Type, r.Title, r.OrderIndex, ParseNode(r.ContentJson),
                r.Sub is null
                    ? null
                    : new ExerciseResultDto(ParseNode(r.Sub.AnswersJson), r.Sub.Score, r.Sub.Total, r.Sub.IsCompleted)))
            .ToList();

        return Result<IReadOnlyList<ExerciseWithResultDto>>.Ok(items);
    }

    public async Task<Result<SubmitResultDto>> Handle(SubmitExerciseAnswerCommand request, CancellationToken ct)
    {
        var ex = await db.LessonExercises.AsNoTracking()
            .Where(e => e.Id == request.LessonExerciseId)
            .Select(e => new { e.Type, e.ContentJson })
            .FirstOrDefaultAsync(ct);
        if (ex is null) return Result<SubmitResultDto>.Fail("NOT_FOUND", "Exercise not found.");

        var (score, total) = ExerciseChecker.Check(ex.Type, ex.ContentJson, request.Answers);
        var answersJson = request.Answers.ValueKind == JsonValueKind.Undefined ? "{}" : request.Answers.GetRawText();

        return await db.ExecuteInTransactionAsync<SubmitResultDto>(async () =>
        {
            var sub = await db.LessonExerciseSubmissions.FirstOrDefaultAsync(
                s => s.LessonExerciseId == request.LessonExerciseId && s.UserId == request.UserId, ct);
            if (sub is null)
            {
                sub = new LessonExerciseSubmission(request.LessonExerciseId, request.UserId, answersJson, score, total);
                await db.LessonExerciseSubmissions.AddAsync(sub, ct);
            }
            else
            {
                sub.Apply(answersJson, score, total);
            }

            await db.SaveChangesAsync(ct);
            return Result<SubmitResultDto>.Ok(new SubmitResultDto(score, total));
        }, ct);
    }

    public async Task<Result<LessonExerciseResultsDto>> Handle(
        GetLessonExerciseResultsQuery request, CancellationToken ct)
    {
        var exercises = await db.LessonExercises.AsNoTracking()
            .Where(e => e.LessonId == request.LessonId)
            .OrderBy(e => e.OrderIndex)
            .Select(e => new ExerciseResultsHeaderDto(e.Id, e.Title, e.Type, e.OrderIndex))
            .ToListAsync(ct);
        var exerciseIds = exercises.Select(e => e.Id).ToList();

        // The class's active students → (userId, name).
        var students = await db.Enrollments.AsNoTracking()
            .Where(en => en.ClassId == request.ClassId && en.Status != EnrollmentStatus.Dropped)
            .Join(db.StudentProfiles, en => en.StudentProfileId, sp => sp.Id,
                (en, sp) => new { sp.UserId, sp.FirstName, sp.LastName })
            .ToListAsync(ct);
        var userIds = students.Select(s => s.UserId).ToList();

        var subs = exerciseIds.Count == 0 || userIds.Count == 0
            ? new List<StudentSub>()
            : await db.LessonExerciseSubmissions.AsNoTracking()
                .Where(s => exerciseIds.Contains(s.LessonExerciseId) && userIds.Contains(s.UserId))
                .Select(s => new StudentSub(s.UserId, s.LessonExerciseId, s.Score, s.Total, s.IsCompleted))
                .ToListAsync(ct);
        var byUser = subs.GroupBy(s => s.UserId).ToDictionary(g => g.Key, g => g.ToList());

        var summaries = students
            .Select(s =>
            {
                var mine = byUser.TryGetValue(s.UserId, out var list) ? list : new List<StudentSub>();
                var results = mine
                    .Select(r => new StudentExerciseResultDto(r.ExerciseId, r.Score, r.Total, r.IsCompleted))
                    .ToList();
                var name = $"{s.FirstName} {s.LastName}".Trim();
                return new StudentExerciseSummaryDto(
                    s.UserId, string.IsNullOrWhiteSpace(name) ? "Student" : name,
                    results.Count(r => r.IsCompleted), exercises.Count, results);
            })
            .OrderBy(s => s.StudentName)
            .ToList();

        return Result<LessonExerciseResultsDto>.Ok(new LessonExerciseResultsDto(request.LessonId, exercises, summaries));
    }

    private sealed record StudentSub(Guid UserId, Guid ExerciseId, int Score, int Total, bool IsCompleted);

    private static JsonNode? ParseNode(string? json)
        => string.IsNullOrWhiteSpace(json) ? null : JsonNode.Parse(json);
}
