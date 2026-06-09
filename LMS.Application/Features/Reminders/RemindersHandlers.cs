using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Reminders;

public sealed class RemindersHandlers(IApplicationDbContext db) :
    IRequestHandler<GetMyRemindersQuery, Result<IReadOnlyCollection<ReminderDto>>>,
    IRequestHandler<CreateReminderCommand, Result<ReminderDto>>,
    IRequestHandler<UpdateReminderCommand, Result<ReminderDto>>,
    IRequestHandler<SetReminderCompletedCommand, Result<ReminderDto>>,
    IRequestHandler<DeleteReminderCommand, Result>
{
    public async Task<Result<IReadOnlyCollection<ReminderDto>>> Handle(GetMyRemindersQuery request, CancellationToken ct)
    {
        var query = db.Reminders.AsNoTracking().Where(r => r.OwnerUserId == request.OwnerUserId);
        if (!request.IncludeCompleted) query = query.Where(r => !r.IsCompleted);
        // Pending items first (sorted by due date), then completed items.
        var items = await query
            .OrderBy(r => r.IsCompleted).ThenBy(r => r.DueAt)
            .Select(r => new ReminderDto(r.Id, r.Title, r.Notes, r.DueAt, r.IsCompleted, r.CompletedAt, r.CreatedAt))
            .ToListAsync(ct);
        return Result<IReadOnlyCollection<ReminderDto>>.Ok(items);
    }

    public async Task<Result<ReminderDto>> Handle(CreateReminderCommand request, CancellationToken ct)
    {
        var entity = new Reminder(request.OwnerUserId, request.Title, request.Notes, request.DueAt);
        await db.Reminders.AddAsync(entity, ct);
        await db.SaveChangesAsync(ct);
        return Result<ReminderDto>.Ok(Map(entity));
    }

    public async Task<Result<ReminderDto>> Handle(UpdateReminderCommand request, CancellationToken ct)
    {
        var entity = await db.Reminders.FirstOrDefaultAsync(r => r.Id == request.ReminderId, ct);
        if (entity is null) return Result<ReminderDto>.Fail("NOT_FOUND", "Reminder not found.");
        // Self-only — refuse if the caller isn't the owner.
        if (entity.OwnerUserId != request.OwnerUserId)
            return Result<ReminderDto>.Fail("FORBIDDEN", "Reminders are self-only.");
        entity.UpdateContent(request.Title, request.Notes, request.DueAt);
        await db.SaveChangesAsync(ct);
        return Result<ReminderDto>.Ok(Map(entity));
    }

    public async Task<Result<ReminderDto>> Handle(SetReminderCompletedCommand request, CancellationToken ct)
    {
        var entity = await db.Reminders.FirstOrDefaultAsync(r => r.Id == request.ReminderId, ct);
        if (entity is null) return Result<ReminderDto>.Fail("NOT_FOUND", "Reminder not found.");
        if (entity.OwnerUserId != request.OwnerUserId)
            return Result<ReminderDto>.Fail("FORBIDDEN", "Reminders are self-only.");
        if (request.IsCompleted) entity.MarkCompleted();
        else entity.MarkPending();
        await db.SaveChangesAsync(ct);
        return Result<ReminderDto>.Ok(Map(entity));
    }

    public async Task<Result> Handle(DeleteReminderCommand request, CancellationToken ct)
    {
        var entity = await db.Reminders.FirstOrDefaultAsync(r => r.Id == request.ReminderId, ct);
        if (entity is null) return Result.Fail("NOT_FOUND", "Reminder not found.");
        if (entity.OwnerUserId != request.OwnerUserId)
            return Result.Fail("FORBIDDEN", "Reminders are self-only.");
        db.Reminders.Remove(entity);
        await db.SaveChangesAsync(ct);
        return Result.Ok("Deleted");
    }

    private static ReminderDto Map(Reminder r) => new(
        r.Id, r.Title, r.Notes, r.DueAt, r.IsCompleted, r.CompletedAt, r.CreatedAt);
}
