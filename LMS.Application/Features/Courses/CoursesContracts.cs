using LMS.Application.Common.Models;
using MediatR;

namespace LMS.Application.Features.Courses;

public sealed record CourseDto(Guid Id, string Code, string Name);

public sealed record CreateCourseCommand(string Code, string Name) : IRequest<Result<CourseDto>>;

public sealed record UpdateCourseCommand(Guid CourseId, string Code, string Name) : IRequest<Result<CourseDto>>;

public sealed record DeleteCourseCommand(Guid CourseId) : IRequest<Result>;

public sealed record GetCoursesQuery : IRequest<Result<IReadOnlyCollection<CourseDto>>>;

public sealed record GetCourseByIdQuery(Guid CourseId) : IRequest<Result<CourseDto>>;