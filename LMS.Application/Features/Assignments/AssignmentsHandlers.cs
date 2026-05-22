using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Assignments;

public sealed class AssignmentsHandlers(IApplicationDbContext db) :
    IRequestHandler<CreateAssignmentCommand, Result<AssignmentDto>>,
    IRequestHandler<UpdateAssignmentCommand, Result<AssignmentDto>>,
    IRequestHandler<PublishAssignmentCommand, Result<AssignmentDto>>,
    IRequestHandler<CloseAssignmentCommand, Result<AssignmentDto>>,
    IRequestHandler<GetClassAssignmentsQuery, Result<IReadOnlyCollection<AssignmentDto>>>,
    IRequestHandler<GetStudentAssignmentsQuery, Result<IReadOnlyCollection<AssignmentDto>>>,
    IRequestHandler<GetAssignmentsQuery, Result<IReadOnlyCollection<AssignmentDto>>>
{
    public async Task<Result<AssignmentDto>> Handle(CloseAssignmentCommand request, CancellationToken cancellationToken)
    {
        var a = await db.Assignments.FirstOrDefaultAsync(x => x.Id == request.AssignmentId, cancellationToken);
        if (a is null) return Result<AssignmentDto>.Fail("NOT_FOUND", "Assignment not found.");
        a.Close();
        await db.SaveChangesAsync(cancellationToken);
        return Result<AssignmentDto>.Ok(Map(a));
    }

    public async Task<Result<AssignmentDto>> Handle(CreateAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var t = await db.Users.FirstOrDefaultAsync(x => x.Id == request.TeacherUserId, cancellationToken);
        if (t is null) return Result<AssignmentDto>.Fail("NOT_FOUND", "Teacher not found.");
        var a = new Assignment(request.ClassId, request.Title, t);
        await db.Assignments.AddAsync(a, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<AssignmentDto>.Ok(Map(a));
    }

    public async Task<Result<IReadOnlyCollection<AssignmentDto>>> Handle(GetClassAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<AssignmentDto>>.Ok(await db.Assignments
            .Where(x => x.ClassId == request.ClassId)
            .Select(a => new AssignmentDto(a.Id, a.ClassId, a.Title, a.Status, a.CreatedByTeacherId))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<IReadOnlyCollection<AssignmentDto>>> Handle(GetStudentAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var cls = await db.Enrollments.Where(x => x.StudentProfileId == request.StudentProfileId).Select(x => x.ClassId)
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<AssignmentDto>>.Ok(await db.Assignments.Where(x => cls.Contains(x.ClassId))
            .Select(a => new AssignmentDto(a.Id, a.ClassId, a.Title, a.Status, a.CreatedByTeacherId))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<AssignmentDto>> Handle(PublishAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var a = await db.Assignments.FirstOrDefaultAsync(x => x.Id == request.AssignmentId, cancellationToken);
        if (a is null) return Result<AssignmentDto>.Fail("NOT_FOUND", "Assignment not found.");
        a.Publish();
        await db.SaveChangesAsync(cancellationToken);
        return Result<AssignmentDto>.Ok(Map(a));
    }

    public async Task<Result<AssignmentDto>> Handle(UpdateAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var a = await db.Assignments.FirstOrDefaultAsync(x => x.Id == request.AssignmentId, cancellationToken);
        if (a is null) return Result<AssignmentDto>.Fail("NOT_FOUND", "Assignment not found.");
        typeof(Assignment).GetProperty(nameof(Assignment.Title))!.SetValue(a, request.Title.Trim());
        await db.SaveChangesAsync(cancellationToken);
        return Result<AssignmentDto>.Ok(Map(a));
    }

    public async Task<Result<IReadOnlyCollection<AssignmentDto>>> Handle(GetAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var q = db.Assignments.AsQueryable();
        if (request.TeacherUserId is { } teacherId)
            q = q.Where(x => x.CreatedByTeacherId == teacherId);
        if (request.ClassId is { } classId)
            q = q.Where(x => x.ClassId == classId);
        if (request.Status is { } status)
            q = q.Where(x => x.Status == status);

        var data = await q
            .OrderByDescending(x => x.CreatedAt)
            .Select(a => new AssignmentDto(a.Id, a.ClassId, a.Title, a.Status, a.CreatedByTeacherId))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<AssignmentDto>>.Ok(data);
    }

    private static AssignmentDto Map(Assignment a)
    {
        return new AssignmentDto(a.Id, a.ClassId, a.Title, a.Status, a.CreatedByTeacherId);
    }
}