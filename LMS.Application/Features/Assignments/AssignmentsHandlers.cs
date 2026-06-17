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
    IRequestHandler<GetAssignmentsQuery, Result<IReadOnlyCollection<AssignmentDto>>>,
    IRequestHandler<AttachBookToAssignmentCommand, Result<AssignmentBookDto>>,
    IRequestHandler<DetachBookFromAssignmentCommand, Result>,
    IRequestHandler<GetAssignmentBooksQuery, Result<IReadOnlyCollection<AssignmentBookDto>>>,
    IRequestHandler<SetAssignmentAssigneesCommand, Result<IReadOnlyCollection<AssignmentAssigneeDto>>>,
    IRequestHandler<GetAssignmentAssigneesQuery, Result<IReadOnlyCollection<AssignmentAssigneeDto>>>
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
        if (request.DueDate.HasValue)
            a.SetDueDate(DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc));
        a.SetDescription(request.Description);
        await db.Assignments.AddAsync(a, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<AssignmentDto>.Ok(Map(a));
    }

    public async Task<Result<IReadOnlyCollection<AssignmentDto>>> Handle(GetClassAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<AssignmentDto>>.Ok(await db.Assignments
            .Where(x => x.ClassId == request.ClassId)
            .Select(a => new AssignmentDto(a.Id, a.ClassId, a.Title, a.Status, a.CreatedByTeacherId, a.DueDate, a.Description))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<IReadOnlyCollection<AssignmentDto>>> Handle(GetStudentAssignmentsQuery request,
        CancellationToken cancellationToken)
    {
        var enrolledClassIds = await db.Enrollments
            .Where(x => x.StudentProfileId == request.StudentProfileId)
            .Select(x => x.ClassId)
            .ToListAsync(cancellationToken);

        // An assignment is visible to this student if:
        //  • The student is enrolled in the assignment's class AND
        //    (the assignment has NO assignee restriction
        //     OR the student is in the assignee set)
        // No assignee rows = whole class. Some rows = targeted subset.
        var data = await db.Assignments
            .Where(a => enrolledClassIds.Contains(a.ClassId))
            .Where(a =>
                !db.AssignmentAssignees.Any(aa => aa.AssignmentId == a.Id) ||
                db.AssignmentAssignees.Any(aa =>
                    aa.AssignmentId == a.Id && aa.StudentProfileId == request.StudentProfileId))
            .Select(a => new AssignmentDto(a.Id, a.ClassId, a.Title, a.Status, a.CreatedByTeacherId, a.DueDate, a.Description))
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyCollection<AssignmentDto>>.Ok(data);
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
        a.UpdateTitle(request.Title);
        a.SetDueDate(request.DueDate.HasValue
            ? DateTime.SpecifyKind(request.DueDate.Value, DateTimeKind.Utc)
            : null);
        a.SetDescription(request.Description);
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
            .Select(a => new AssignmentDto(a.Id, a.ClassId, a.Title, a.Status, a.CreatedByTeacherId, a.DueDate, a.Description))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<AssignmentDto>>.Ok(data);
    }

    // ----- Book attachments ------------------------------------------------

    public async Task<Result<AssignmentBookDto>> Handle(AttachBookToAssignmentCommand request,
        CancellationToken cancellationToken)
    {
        var assignmentExists = await db.Assignments.AnyAsync(a => a.Id == request.AssignmentId, cancellationToken);
        if (!assignmentExists) return Result<AssignmentBookDto>.Fail("NOT_FOUND", "Assignment not found.");
        var bookExists = await db.Books.AnyAsync(b => b.Id == request.BookId, cancellationToken);
        if (!bookExists) return Result<AssignmentBookDto>.Fail("NOT_FOUND", "Book not found.");

        var existing = await db.AssignmentBooks.FirstOrDefaultAsync(
            x => x.AssignmentId == request.AssignmentId && x.BookId == request.BookId,
            cancellationToken);
        if (existing is not null)
        {
            existing.SetNote(request.Note);
            await db.SaveChangesAsync(cancellationToken);
            return Result<AssignmentBookDto>.Ok(
                new AssignmentBookDto(existing.Id, existing.BookId, existing.Note),
                "Note updated.");
        }

        var link = new AssignmentBook(request.AssignmentId, request.BookId, request.Note);
        await db.AssignmentBooks.AddAsync(link, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<AssignmentBookDto>.Ok(new AssignmentBookDto(link.Id, link.BookId, link.Note));
    }

    public async Task<Result> Handle(DetachBookFromAssignmentCommand request, CancellationToken cancellationToken)
    {
        var link = await db.AssignmentBooks.FirstOrDefaultAsync(
            x => x.AssignmentId == request.AssignmentId && x.BookId == request.BookId,
            cancellationToken);
        if (link is null) return Result.Fail("NOT_FOUND", "Book is not attached to this assignment.");
        db.AssignmentBooks.Remove(link);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Book detached.");
    }

    public async Task<Result<IReadOnlyCollection<AssignmentBookDto>>> Handle(
        GetAssignmentBooksQuery request, CancellationToken cancellationToken)
    {
        var items = await db.AssignmentBooks
            .Where(x => x.AssignmentId == request.AssignmentId)
            .Select(x => new AssignmentBookDto(x.Id, x.BookId, x.Note))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<AssignmentBookDto>>.Ok(items);
    }

    // ----- Per-student targeting ------------------------------------------

    public async Task<Result<IReadOnlyCollection<AssignmentAssigneeDto>>> Handle(
        SetAssignmentAssigneesCommand request, CancellationToken cancellationToken)
    {
        var assignment = await db.Assignments.FirstOrDefaultAsync(a => a.Id == request.AssignmentId, cancellationToken);
        if (assignment is null)
            return Result<IReadOnlyCollection<AssignmentAssigneeDto>>.Fail("NOT_FOUND", "Assignment not found.");

        // Only allow assignees who are actually enrolled in the assignment's class.
        var enrolled = await db.Enrollments
            .Where(e => e.ClassId == assignment.ClassId)
            .Select(e => e.StudentProfileId)
            .ToListAsync(cancellationToken);
        var enrolledSet = enrolled.ToHashSet();

        var requested = request.StudentProfileIds.Where(id => enrolledSet.Contains(id)).Distinct().ToList();

        var existing = await db.AssignmentAssignees
            .Where(a => a.AssignmentId == request.AssignmentId)
            .ToListAsync(cancellationToken);
        var existingSet = existing.Select(e => e.StudentProfileId).ToHashSet();

        var toRemove = existing.Where(e => !requested.Contains(e.StudentProfileId)).ToList();
        var toAdd = requested.Where(id => !existingSet.Contains(id))
            .Select(id => new AssignmentAssignee(request.AssignmentId, id))
            .ToList();

        if (toRemove.Count > 0) db.AssignmentAssignees.RemoveRange(toRemove);
        if (toAdd.Count > 0) await db.AssignmentAssignees.AddRangeAsync(toAdd, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        var final = requested
            .Select(id => new AssignmentAssigneeDto(request.AssignmentId, id))
            .ToList();
        return Result<IReadOnlyCollection<AssignmentAssigneeDto>>.Ok(final);
    }

    public async Task<Result<IReadOnlyCollection<AssignmentAssigneeDto>>> Handle(
        GetAssignmentAssigneesQuery request, CancellationToken cancellationToken)
    {
        var items = await db.AssignmentAssignees
            .Where(a => a.AssignmentId == request.AssignmentId)
            .Select(a => new AssignmentAssigneeDto(a.AssignmentId, a.StudentProfileId))
            .ToListAsync(cancellationToken);
        return Result<IReadOnlyCollection<AssignmentAssigneeDto>>.Ok(items);
    }

    private static AssignmentDto Map(Assignment a)
    {
        return new AssignmentDto(a.Id, a.ClassId, a.Title, a.Status, a.CreatedByTeacherId, a.DueDate, a.Description);
    }
}