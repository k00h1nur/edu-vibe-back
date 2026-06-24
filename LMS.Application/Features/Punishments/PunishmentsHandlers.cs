using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using LMS.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Punishments;

/// <summary>
/// Admin-only teacher-punishment CRUD. The applying admin is taken from the
/// authenticated user (controller is permission-gated to <c>Punishments.Manage</c>).
/// Punishments feed the F5 salary calculation.
/// </summary>
public sealed class PunishmentsHandlers(IApplicationDbContext db, ICurrentUserService currentUser) :
    IRequestHandler<CreatePunishmentCommand, Result<PunishmentDto>>,
    IRequestHandler<UpdatePunishmentCommand, Result<PunishmentDto>>,
    IRequestHandler<DeletePunishmentCommand, Result>,
    IRequestHandler<GetPunishmentByIdQuery, Result<PunishmentDto>>,
    IRequestHandler<GetPunishmentsQuery, Result<IReadOnlyCollection<PunishmentDto>>>
{
    public async Task<Result<PunishmentDto>> Handle(CreatePunishmentCommand request, CancellationToken ct)
    {
        if (currentUser.UserId is not { } adminId)
            return Result<PunishmentDto>.Fail("FORBIDDEN", "Not signed in.");
        if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == request.TeacherId, ct))
            return Result<PunishmentDto>.Fail("NOT_FOUND", "Teacher not found.");

        try
        {
            var p = new Punishment(request.TeacherId, request.Title, request.Description, request.Type,
                request.Value, request.Reason, adminId, request.PeriodMonth);
            await db.Punishments.AddAsync(p, ct);
            await db.SaveChangesAsync(ct);
            return Result<PunishmentDto>.Ok(Map(p), "Punishment applied.");
        }
        catch (DomainException ex)
        {
            return Result<PunishmentDto>.Fail("VALIDATION", ex.Message);
        }
    }

    public async Task<Result<PunishmentDto>> Handle(UpdatePunishmentCommand request, CancellationToken ct)
    {
        var p = await db.Punishments.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (p is null) return Result<PunishmentDto>.Fail("NOT_FOUND", "Punishment not found.");
        try
        {
            p.Update(request.Title, request.Description, request.Type, request.Value, request.Reason, request.PeriodMonth);
            await db.SaveChangesAsync(ct);
            return Result<PunishmentDto>.Ok(Map(p), "Updated.");
        }
        catch (DomainException ex)
        {
            return Result<PunishmentDto>.Fail("VALIDATION", ex.Message);
        }
    }

    public async Task<Result> Handle(DeletePunishmentCommand request, CancellationToken ct)
    {
        var p = await db.Punishments.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (p is null) return Result.Fail("NOT_FOUND", "Punishment not found.");
        db.Punishments.Remove(p);
        await db.SaveChangesAsync(ct);
        return Result.Ok("Deleted.");
    }

    public async Task<Result<PunishmentDto>> Handle(GetPunishmentByIdQuery request, CancellationToken ct)
    {
        var p = await db.Punishments.AsNoTracking().FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        return p is null
            ? Result<PunishmentDto>.Fail("NOT_FOUND", "Punishment not found.")
            : Result<PunishmentDto>.Ok(Map(p));
    }

    public async Task<Result<IReadOnlyCollection<PunishmentDto>>> Handle(GetPunishmentsQuery request, CancellationToken ct)
    {
        var q = db.Punishments.AsNoTracking().AsQueryable();
        if (request.TeacherId is { } tid) q = q.Where(x => x.TeacherId == tid);
        if (request.Month is { } m)
        {
            var first = new DateOnly(m.Year, m.Month, 1);
            q = q.Where(x => x.PeriodMonth == first);
        }
        var rows = await q.OrderByDescending(x => x.PeriodMonth).ThenByDescending(x => x.CreatedAt).ToListAsync(ct);
        return Result<IReadOnlyCollection<PunishmentDto>>.Ok(rows.Select(Map).ToList());
    }

    private static PunishmentDto Map(Punishment p) => new(
        p.Id, p.TeacherId, p.Title, p.Description, p.Type, p.Value, p.Reason,
        p.AppliedByAdminId, p.PeriodMonth, p.CreatedAt);
}
