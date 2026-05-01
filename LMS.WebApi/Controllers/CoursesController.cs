using LMS.Application.Features.Courses;
using LMS.WebApi.Common;
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
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CourseDto>>>> GetAll(CancellationToken ct)
    {
        var r = await sender.Send(new GetCoursesQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<CourseDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CourseDto>>> Get(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetCourseByIdQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<CourseDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<CourseDto>.Fail(r.Message ?? "Not found"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CourseDto>>> Create([FromBody] CreateCourseCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<CourseDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<CourseDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CourseDto>>> Update(Guid id, [FromBody] UpdateCourseCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { CourseId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<CourseDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<CourseDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new DeleteCourseCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}