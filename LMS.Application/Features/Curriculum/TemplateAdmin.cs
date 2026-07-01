using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Curriculum;

// ===== DTO ==================================================================

/// <summary>A template row for the admin management list — includes the published flag
/// and how many classes currently use it (drives the delete guard).</summary>
public sealed record AdminTemplateDto(
    Guid Id, string Name, CurriculumCategory Category, string? Level, string? Description,
    bool IsPublished, bool IsSystem, int ModuleCount, int UnitCount, int LessonCount, int ClassCount);

// ===== Use cases ===========================================================

/// <summary>Admin: ALL master (IsSystem) templates — published AND unpublished — with
/// structure counts + class-usage count. The picker query stays published-only.</summary>
public sealed record GetAdminTemplatesQuery : IRequest<Result<IReadOnlyList<AdminTemplateDto>>>;

/// <summary>Admin: edit a template's metadata + its published flag.</summary>
public sealed record UpdateTemplateCommand(
    Guid Id, string Name, CurriculumCategory Category, string? Level, string? Description, bool IsPublished)
    : IRequest<Result<AdminTemplateDto>>;

/// <summary>Admin: delete a template. BLOCKED while any class uses it — deleting would
/// break those classes and cascade-destroy their lessons + students' exercise answers.</summary>
public sealed record DeleteTemplateCommand(Guid Id) : IRequest<Result<bool>>;

// ===== Handlers ============================================================

public sealed class TemplateAdminHandlers(IApplicationDbContext db) :
    IRequestHandler<GetAdminTemplatesQuery, Result<IReadOnlyList<AdminTemplateDto>>>,
    IRequestHandler<UpdateTemplateCommand, Result<AdminTemplateDto>>,
    IRequestHandler<DeleteTemplateCommand, Result<bool>>
{
    // Structure + usage counts for one template id, as EF-translatable subqueries.
    private IQueryable<AdminTemplateDto> ProjectAdminDto(IQueryable<CurriculumTemplate> q) =>
        q.Select(t => new AdminTemplateDto(
            t.Id, t.Name, t.Category, t.Level, t.Description, t.IsPublished, t.IsSystem,
            db.CurriculumModules.Count(m => m.TemplateId == t.Id),
            db.CurriculumUnits.Count(u => db.CurriculumModules.Any(m => m.Id == u.ModuleId && m.TemplateId == t.Id)),
            db.CurriculumLessons.Count(l => db.CurriculumUnits.Any(u =>
                u.Id == l.UnitId && db.CurriculumModules.Any(m => m.Id == u.ModuleId && m.TemplateId == t.Id))),
            db.Classes.Count(c => c.CurriculumTemplateId == t.Id)));

    public async Task<Result<IReadOnlyList<AdminTemplateDto>>> Handle(GetAdminTemplatesQuery request, CancellationToken ct)
    {
        var rows = await ProjectAdminDto(
                db.CurriculumTemplates.AsNoTracking().Where(t => t.IsSystem).OrderBy(t => t.Category).ThenBy(t => t.Name))
            .ToListAsync(ct);
        return Result<IReadOnlyList<AdminTemplateDto>>.Ok(rows);
    }

    public async Task<Result<AdminTemplateDto>> Handle(UpdateTemplateCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return Result<AdminTemplateDto>.Fail("VALIDATION", "Name is required.");

        var t = await db.CurriculumTemplates.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (t is null) return Result<AdminTemplateDto>.Fail("NOT_FOUND", "Template not found.");

        t.Update(request.Name, request.Category, request.Level, request.Description);
        t.SetPublished(request.IsPublished);
        await db.SaveChangesAsync(ct);

        var dto = await ProjectAdminDto(db.CurriculumTemplates.AsNoTracking().Where(x => x.Id == t.Id)).FirstAsync(ct);
        return Result<AdminTemplateDto>.Ok(dto);
    }

    public async Task<Result<bool>> Handle(DeleteTemplateCommand request, CancellationToken ct)
    {
        var t = await db.CurriculumTemplates.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (t is null) return Result<bool>.Fail("NOT_FOUND", "Template not found.");

        var usedBy = await db.Classes.CountAsync(c => c.CurriculumTemplateId == request.Id, ct);
        if (usedBy > 0)
            return Result<bool>.Fail("IN_USE",
                $"This template is used by {usedBy} class(es). Unassign it from those classes before deleting.");

        db.CurriculumTemplates.Remove(t);
        await db.SaveChangesAsync(ct);
        return Result<bool>.Ok(true);
    }
}
