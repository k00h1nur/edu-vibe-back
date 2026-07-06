using LMS.Application.Common.Security;
using LMS.Application.Features.Courses;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CoursesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [PermissionAuthorize(Permissions.Courses.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CourseDto>>>> GetAll(CancellationToken ct)
    {
        var r = await sender.Send(new GetCoursesQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<CourseDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}")]
    [PermissionAuthorize(Permissions.Courses.Read)]
    public async Task<ActionResult<ApiResponse<CourseDto>>> Get(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetCourseByIdQuery(id), ct);
        return r.ToApiResultOrNotFound();
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Courses.Manage)]
    public async Task<ActionResult<ApiResponse<CourseDto>>> Create([FromBody] CreateCourseCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.ToApiResult();
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Courses.Manage)]
    public async Task<ActionResult<ApiResponse<CourseDto>>> Update(Guid id, [FromBody] UpdateCourseCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { CourseId = id }, ct);
        return r.ToApiResult();
    }

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(Permissions.Courses.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new DeleteCourseCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}
