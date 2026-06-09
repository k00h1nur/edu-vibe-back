using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Materials;

public sealed class MaterialsHandlers(IApplicationDbContext db) :
    IRequestHandler<CreateMaterialCommand, Result<MaterialDto>>,
    IRequestHandler<UpdateMaterialDetailsCommand, Result<MaterialDto>>,
    IRequestHandler<DeleteMaterialCommand, Result<string>>,
    IRequestHandler<GetMaterialsQuery, Result<IReadOnlyCollection<MaterialDto>>>,
    IRequestHandler<GetMaterialByIdQuery, Result<MaterialDto>>
{
    public async Task<Result<MaterialDto>> Handle(CreateMaterialCommand request, CancellationToken ct)
    {
        var material = new Material(
            request.Title,
            request.Description,
            request.Visibility,
            request.StoredFileName,
            request.OriginalFileName,
            request.MimeType,
            request.FileSize,
            request.UploadedByUserId);

        // Private materials must list at least one class — without it nobody
        // can see them. Public materials ignore the class list.
        if (request.Visibility == MaterialVisibility.Private)
        {
            if (request.ClassIds.Count == 0)
                return Result<MaterialDto>.Fail("VALIDATION", "Private materials require at least one class.");

            foreach (var classId in request.ClassIds.Distinct())
            {
                material.Classes.Add(new MaterialClass(material.Id, classId));
            }
        }

        await db.Materials.AddAsync(material, ct);
        await db.SaveChangesAsync(ct);
        return Result<MaterialDto>.Ok(Map(material));
    }

    public async Task<Result<MaterialDto>> Handle(UpdateMaterialDetailsCommand request, CancellationToken ct)
    {
        var material = await db.Materials
            .Include(m => m.Classes)
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId, ct);
        if (material is null) return Result<MaterialDto>.Fail("NOT_FOUND", "Material not found.");

        material.UpdateDetails(request.Title, request.Description);
        if (material.Visibility != request.Visibility)
        {
            material.ChangeVisibility(request.Visibility);
        }

        // Reconcile class links to match desired state.
        var desired = request.Visibility == MaterialVisibility.Private
            ? request.ClassIds.Distinct().ToHashSet()
            : new HashSet<Guid>();
        var existing = material.Classes.Select(c => c.ClassId).ToHashSet();

        foreach (var link in material.Classes.Where(c => !desired.Contains(c.ClassId)).ToList())
        {
            material.Classes.Remove(link);
        }
        foreach (var id in desired.Where(id => !existing.Contains(id)))
        {
            material.Classes.Add(new MaterialClass(material.Id, id));
        }

        if (request.Visibility == MaterialVisibility.Private && desired.Count == 0)
            return Result<MaterialDto>.Fail("VALIDATION", "Private materials require at least one class.");

        await db.SaveChangesAsync(ct);
        return Result<MaterialDto>.Ok(Map(material));
    }

    public async Task<Result<string>> Handle(DeleteMaterialCommand request, CancellationToken ct)
    {
        var material = await db.Materials.FirstOrDefaultAsync(m => m.Id == request.MaterialId, ct);
        if (material is null) return Result<string>.Fail("NOT_FOUND", "Material not found.");
        // Return the stored file name so the controller can delete the file
        // after the row is gone — pre-delete sequencing prevents orphan rows
        // if file deletion races with another request.
        var fileName = material.StoredFileName;
        db.Materials.Remove(material);
        await db.SaveChangesAsync(ct);
        return Result<string>.Ok(fileName);
    }

    public async Task<Result<IReadOnlyCollection<MaterialDto>>> Handle(GetMaterialsQuery request, CancellationToken ct)
    {
        var query = db.Materials.AsNoTracking().Include(m => m.Classes).AsQueryable();

        if (!request.IsAdmin)
        {
            // Non-admins see Public materials + Private materials linked to a
            // class they're enrolled in OR teaching.
            var teaches = await db.Classes.AsNoTracking()
                .Where(c => c.TeacherUserId == request.CallerUserId)
                .Select(c => c.Id)
                .ToListAsync(ct);
            var attendsAsStudent = await db.StudentProfiles.AsNoTracking()
                .Where(sp => sp.UserId == request.CallerUserId)
                .SelectMany(sp => db.Enrollments.Where(e => e.StudentProfileId == sp.Id).Select(e => e.ClassId))
                .ToListAsync(ct);
            var visibleClassIds = teaches.Concat(attendsAsStudent).Distinct().ToHashSet();

            query = query.Where(m =>
                m.Visibility == MaterialVisibility.Public ||
                m.Classes.Any(mc => visibleClassIds.Contains(mc.ClassId)));
        }

        if (request.ClassId.HasValue)
        {
            var cid = request.ClassId.Value;
            query = query.Where(m =>
                m.Visibility == MaterialVisibility.Public ||
                m.Classes.Any(mc => mc.ClassId == cid));
        }

        var items = await query
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new MaterialDto(
                m.Id, m.Title, m.Description, m.Visibility,
                m.OriginalFileName, m.MimeType, m.FileSize,
                m.UploadedByUserId, m.CreatedAt,
                m.Classes.Select(c => c.ClassId).ToList()))
            .ToListAsync(ct);
        return Result<IReadOnlyCollection<MaterialDto>>.Ok(items);
    }

    public async Task<Result<MaterialDto>> Handle(GetMaterialByIdQuery request, CancellationToken ct)
    {
        var material = await db.Materials.AsNoTracking()
            .Include(m => m.Classes)
            .FirstOrDefaultAsync(m => m.Id == request.MaterialId, ct);
        if (material is null) return Result<MaterialDto>.Fail("NOT_FOUND", "Material not found.");

        if (!request.IsAdmin && material.Visibility == MaterialVisibility.Private)
        {
            // Authorize: caller must teach or attend at least one of the
            // linked classes.
            var linkedClassIds = material.Classes.Select(c => c.ClassId).ToList();
            var teaches = await db.Classes.AsNoTracking()
                .AnyAsync(c => linkedClassIds.Contains(c.Id) && c.TeacherUserId == request.CallerUserId, ct);
            if (!teaches)
            {
                var attendsAsStudent = await db.StudentProfiles.AsNoTracking()
                    .Where(sp => sp.UserId == request.CallerUserId)
                    .SelectMany(sp => db.Enrollments.Where(e => e.StudentProfileId == sp.Id))
                    .AnyAsync(e => linkedClassIds.Contains(e.ClassId), ct);
                if (!attendsAsStudent)
                    return Result<MaterialDto>.Fail("FORBIDDEN", "You don't have access to this material.");
            }
        }

        return Result<MaterialDto>.Ok(Map(material));
    }

    private static MaterialDto Map(Material m) => new(
        m.Id, m.Title, m.Description, m.Visibility,
        m.OriginalFileName, m.MimeType, m.FileSize,
        m.UploadedByUserId, m.CreatedAt,
        m.Classes.Select(c => c.ClassId).ToList());
}
