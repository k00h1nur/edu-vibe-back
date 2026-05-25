using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Courses;

public sealed class CoursesHandlers(IApplicationDbContext db) :
    IRequestHandler<GetCoursesQuery, Result<IReadOnlyCollection<CourseDto>>>,
    IRequestHandler<GetCourseByIdQuery, Result<CourseDto>>,
    IRequestHandler<CreateCourseCommand, Result<CourseDto>>,
    IRequestHandler<UpdateCourseCommand, Result<CourseDto>>,
    IRequestHandler<DeleteCourseCommand, Result>
{
    public async Task<Result<CourseDto>> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        if (await db.Courses.AnyAsync(x => x.Code == request.Code.ToUpper(), cancellationToken))
            return Result<CourseDto>.Fail("DUPLICATE", "Course code exists.");
        var c = new Course(request.Code, request.Name);
        await db.Courses.AddAsync(c, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<CourseDto>.Ok(new CourseDto(c.Id, c.Code, c.Name));
    }

    public async Task<Result> Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
    {
        var c = await db.Courses.FirstOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken);
        if (c is null) return Result.Fail("NOT_FOUND", "Course not found.");
        db.Courses.Remove(c);
        await db.SaveChangesAsync(cancellationToken);
        return Result.Ok("Deleted");
    }

    public async Task<Result<CourseDto>> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var c = await db.Courses.FirstOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken);
        return c is null
            ? Result<CourseDto>.Fail("NOT_FOUND", "Course not found.")
            : Result<CourseDto>.Ok(new CourseDto(c.Id, c.Code, c.Name));
    }

    public async Task<Result<IReadOnlyCollection<CourseDto>>> Handle(GetCoursesQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<CourseDto>>.Ok(await db.Courses
            .Select(x => new CourseDto(x.Id, x.Code, x.Name)).ToListAsync(cancellationToken));
    }

    public async Task<Result<CourseDto>> Handle(UpdateCourseCommand request, CancellationToken cancellationToken)
    {
        var c = await db.Courses.FirstOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken);
        if (c is null) return Result<CourseDto>.Fail("NOT_FOUND", "Course not found.");
        c.Update(request.Code, request.Name);
        await db.SaveChangesAsync(cancellationToken);
        return Result<CourseDto>.Ok(new CourseDto(c.Id, c.Code, c.Name));
    }
}