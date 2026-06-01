using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Classes;

public sealed class ClassesHandlers(IApplicationDbContext db) :
    IRequestHandler<GetClassesQuery, Result<PagedResult<ClassDto>>>,
    IRequestHandler<GetClassByIdQuery, Result<ClassDto>>,
    IRequestHandler<GetAssignedClassesQuery, Result<IReadOnlyCollection<ClassDto>>>,
    IRequestHandler<CreateClassCommand, Result<ClassDto>>,
    IRequestHandler<UpdateClassCommand, Result<ClassDto>>,
    IRequestHandler<CancelClassCommand, Result>,
    IRequestHandler<EnrollStudentCommand, Result>,
    IRequestHandler<RemoveStudentFromClassCommand, Result>,
    IRequestHandler<GetClassStudentsQuery, Result<IReadOnlyCollection<Guid>>>
{
    public async Task<Result> Handle(CancelClassCommand request, CancellationToken cancellationToken)
    {
        var c = await db.Classes.FirstOrDefaultAsync(x => x.Id == request.ClassId, cancellationToken);
        if (c is null) return Result.Fail("NOT_FOUND", "Class not found.");
        c.Cancel();
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Cancelled");
    }

    public async Task<Result<ClassDto>> Handle(CreateClassCommand request, CancellationToken cancellationToken)
    {
        var c = new Class(request.Title, request.MaxStudents, request.Modality);
        if (request.TeacherUserId.HasValue)
        {
            var t = await db.Users.FirstOrDefaultAsync(x => x.Id == request.TeacherUserId.Value, cancellationToken);
            if (t is null) return Result<ClassDto>.Fail("NOT_FOUND", "Teacher not found.");
            c.AssignTeacher(t);
        }

        await db.Classes.AddAsync(c, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<ClassDto>.Ok(Map(c));
    }

    public async Task<Result> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
    {
        var c = await db.Classes.Include(x => x.Enrollments)
            .FirstOrDefaultAsync(x => x.Id == request.ClassId, cancellationToken);
        if (c is null) return Result.Fail("NOT_FOUND", "Class not found.");
        c.EnrollStudent(request.StudentProfileId);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Enrolled");
    }

    public async Task<Result<IReadOnlyCollection<ClassDto>>> Handle(GetAssignedClassesQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<ClassDto>>.Ok(await db.Classes
            .Where(x => x.TeacherUserId == request.TeacherUserId)
            .Select(c => new ClassDto(c.Id, c.Title, c.MaxStudents, c.Modality, c.Status, c.TeacherUserId))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<ClassDto>> Handle(GetClassByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await db.Classes.FirstOrDefaultAsync(x => x.Id == request.ClassId, cancellationToken);
        return c is null ? Result<ClassDto>.Fail("NOT_FOUND", "Class not found.") : Result<ClassDto>.Ok(Map(c));
    }

    public async Task<Result<PagedResult<ClassDto>>> Handle(GetClassesQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize, request.Search);

        var query = db.Classes
            .Select(c => new ClassDto(c.Id, c.Title, c.MaxStudents, c.Modality, c.Status, c.TeacherUserId));

        if (page.NormalizedSearch is { } search)
            query = query.Where(c => c.Title.ToLower().Contains(search));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(c => c.Title)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);

        return Result<PagedResult<ClassDto>>.Ok(PagedResult<ClassDto>.From(items, total, page));
    }

    public async Task<Result<IReadOnlyCollection<Guid>>> Handle(GetClassStudentsQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<Guid>>.Ok(await db.Enrollments
            .Where(x => x.ClassId == request.ClassId && x.Status == EnrollmentStatus.Active)
            .Select(x => x.StudentProfileId).ToListAsync(cancellationToken));
    }

    public async Task<Result> Handle(RemoveStudentFromClassCommand request, CancellationToken cancellationToken)
    {
        var e = await db.Enrollments.FirstOrDefaultAsync(
            x => x.ClassId == request.ClassId && x.StudentProfileId == request.StudentProfileId, cancellationToken);
        if (e is null) return Result.Fail("NOT_FOUND", "Enrollment not found.");
        e.Drop();
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Dropped");
    }

    public async Task<Result<ClassDto>> Handle(UpdateClassCommand request, CancellationToken cancellationToken)
    {
        var c = await db.Classes.FirstOrDefaultAsync(x => x.Id == request.ClassId, cancellationToken);
        if (c is null) return Result<ClassDto>.Fail("NOT_FOUND", "Class not found.");
        c.UpdateDetails(request.Title, request.MaxStudents, request.Modality);
        if (request.TeacherUserId.HasValue)
        {
            var t = await db.Users.FirstOrDefaultAsync(x => x.Id == request.TeacherUserId.Value, cancellationToken);
            if (t is null) return Result<ClassDto>.Fail("NOT_FOUND", "Teacher not found.");
            c.AssignTeacher(t);
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result<ClassDto>.Ok(Map(c));
    }

    private static ClassDto Map(Class c)
    {
        return new ClassDto(c.Id, c.Title, c.MaxStudents, c.Modality, c.Status, c.TeacherUserId);
    }
}