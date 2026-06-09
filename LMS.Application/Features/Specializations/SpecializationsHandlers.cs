using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Specializations;

public sealed class SpecializationsHandlers(IApplicationDbContext db) :
    IRequestHandler<GetSpecializationsQuery, Result<IReadOnlyCollection<SpecializationDto>>>,
    IRequestHandler<CreateSpecializationCommand, Result<SpecializationDto>>,
    IRequestHandler<UpdateSpecializationCommand, Result<SpecializationDto>>,
    IRequestHandler<SetSpecializationActiveCommand, Result<SpecializationDto>>,
    IRequestHandler<DeleteSpecializationCommand, Result>,
    IRequestHandler<SetStaffSpecializationsCommand, Result<IReadOnlyCollection<SpecializationDto>>>,
    IRequestHandler<GetStaffSpecializationsQuery, Result<IReadOnlyCollection<SpecializationDto>>>
{
    public async Task<Result<IReadOnlyCollection<SpecializationDto>>> Handle(GetSpecializationsQuery request,
        CancellationToken ct)
    {
        var query = db.Specializations.AsNoTracking();
        if (!request.IncludeInactive) query = query.Where(s => s.IsActive);
        var items = await query
            .OrderBy(s => s.Name)
            .Select(s => new SpecializationDto(s.Id, s.Code, s.Name, s.IsActive))
            .ToListAsync(ct);
        return Result<IReadOnlyCollection<SpecializationDto>>.Ok(items);
    }

    public async Task<Result<SpecializationDto>> Handle(CreateSpecializationCommand request, CancellationToken ct)
    {
        var code = request.Code.Trim().ToLowerInvariant();
        if (await db.Specializations.AnyAsync(s => s.Code == code, ct))
            return Result<SpecializationDto>.Fail("EXISTS", "A specialization with this code already exists.");

        var spec = new Specialization(request.Code, request.Name);
        await db.Specializations.AddAsync(spec, ct);
        await db.SaveChangesAsync(ct);
        return Result<SpecializationDto>.Ok(new SpecializationDto(spec.Id, spec.Code, spec.Name, spec.IsActive));
    }

    public async Task<Result<SpecializationDto>> Handle(UpdateSpecializationCommand request, CancellationToken ct)
    {
        var spec = await db.Specializations.FirstOrDefaultAsync(s => s.Id == request.SpecializationId, ct);
        if (spec is null) return Result<SpecializationDto>.Fail("NOT_FOUND", "Specialization not found.");
        spec.Rename(request.Name);
        await db.SaveChangesAsync(ct);
        return Result<SpecializationDto>.Ok(new SpecializationDto(spec.Id, spec.Code, spec.Name, spec.IsActive));
    }

    public async Task<Result<SpecializationDto>> Handle(SetSpecializationActiveCommand request, CancellationToken ct)
    {
        var spec = await db.Specializations.FirstOrDefaultAsync(s => s.Id == request.SpecializationId, ct);
        if (spec is null) return Result<SpecializationDto>.Fail("NOT_FOUND", "Specialization not found.");
        if (request.IsActive) spec.Activate(); else spec.Deactivate();
        await db.SaveChangesAsync(ct);
        return Result<SpecializationDto>.Ok(new SpecializationDto(spec.Id, spec.Code, spec.Name, spec.IsActive));
    }

    public async Task<Result> Handle(DeleteSpecializationCommand request, CancellationToken ct)
    {
        var spec = await db.Specializations.FirstOrDefaultAsync(s => s.Id == request.SpecializationId, ct);
        if (spec is null) return Result.Fail("NOT_FOUND", "Specialization not found.");

        // If any staff are linked, refuse hard-delete — call Deactivate instead.
        // Hard delete cascades through StaffSpecialization but losing the link
        // history is rarely what an admin wants. Tell them to deactivate.
        if (await db.StaffSpecializations.AnyAsync(s => s.SpecializationId == spec.Id, ct))
            return Result.Fail("IN_USE",
                "This specialization is assigned to one or more teachers. Deactivate it instead.");

        db.Specializations.Remove(spec);
        await db.SaveChangesAsync(ct);
        return Result.Ok("Deleted");
    }

    public async Task<Result<IReadOnlyCollection<SpecializationDto>>> Handle(SetStaffSpecializationsCommand request,
        CancellationToken ct)
    {
        var staff = await db.StaffProfiles
            .Include(s => s.Specializations)
            .FirstOrDefaultAsync(s => s.Id == request.StaffProfileId, ct);
        if (staff is null)
            return Result<IReadOnlyCollection<SpecializationDto>>.Fail("NOT_FOUND", "Staff profile not found.");

        var desiredIds = request.SpecializationIds.ToHashSet();
        // Validate every id resolves to an active (or at-least existing)
        // specialization. We allow assigning inactive ones — admins shouldn't
        // be blocked from grandfathering historical assignments.
        var validIds = await db.Specializations
            .Where(s => desiredIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);
        if (validIds.Count != desiredIds.Count)
        {
            var missing = desiredIds.Except(validIds);
            return Result<IReadOnlyCollection<SpecializationDto>>.Fail(
                "NOT_FOUND",
                $"Unknown specialization id(s): {string.Join(", ", missing)}");
        }

        // Diff current vs desired. Delete only what needs removing; insert only
        // what's new — avoids the "delete-everything-then-reinsert" anti-pattern
        // that breaks referential history and audit triggers.
        var current = staff.Specializations.ToList();
        foreach (var link in current.Where(l => !desiredIds.Contains(l.SpecializationId)))
        {
            staff.Specializations.Remove(link);
        }
        var existingIds = current.Select(l => l.SpecializationId).ToHashSet();
        foreach (var id in desiredIds.Where(id => !existingIds.Contains(id)))
        {
            staff.Specializations.Add(new StaffSpecialization(staff.Id, id));
        }

        await db.SaveChangesAsync(ct);

        var result = await db.Specializations.AsNoTracking()
            .Where(s => desiredIds.Contains(s.Id))
            .OrderBy(s => s.Name)
            .Select(s => new SpecializationDto(s.Id, s.Code, s.Name, s.IsActive))
            .ToListAsync(ct);
        return Result<IReadOnlyCollection<SpecializationDto>>.Ok(result);
    }

    public async Task<Result<IReadOnlyCollection<SpecializationDto>>> Handle(GetStaffSpecializationsQuery request,
        CancellationToken ct)
    {
        var items = await db.StaffSpecializations.AsNoTracking()
            .Where(l => l.StaffProfileId == request.StaffProfileId)
            .Join(db.Specializations.AsNoTracking(),
                l => l.SpecializationId, s => s.Id, (_, s) => s)
            .OrderBy(s => s.Name)
            .Select(s => new SpecializationDto(s.Id, s.Code, s.Name, s.IsActive))
            .ToListAsync(ct);
        return Result<IReadOnlyCollection<SpecializationDto>>.Ok(items);
    }
}
