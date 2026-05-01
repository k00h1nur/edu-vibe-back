using LMS.Application.Features.Classes;
using LMS.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ClassesController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ClassDto>>>> GetAll(CancellationToken ct)
    {
        var r = await sender.Send(new GetClassesQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<ClassDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClassDto>>> Get(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetClassByIdQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<ClassDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<ClassDto>.Fail(r.Message ?? "Not found"));
    }

    [HttpGet("assigned/{teacherUserId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ClassDto>>>> Assigned(Guid teacherUserId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetAssignedClassesQuery(teacherUserId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<ClassDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}/students")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<Guid>>>> Students(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetClassStudentsQuery(id), ct);
        return Ok(ApiResponse<IReadOnlyCollection<Guid>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ClassDto>>> Create([FromBody] CreateClassCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<ClassDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<ClassDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClassDto>>> Update(Guid id, [FromBody] UpdateClassCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { ClassId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<ClassDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<ClassDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Cancel(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new CancelClassCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/enroll/{studentProfileId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Enroll(Guid id, Guid studentProfileId, CancellationToken ct)
    {
        var r = await sender.Send(new EnrollStudentCommand(id, studentProfileId), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}/enroll/{studentProfileId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Drop(Guid id, Guid studentProfileId, CancellationToken ct)
    {
        var r = await sender.Send(new RemoveStudentFromClassCommand(id, studentProfileId), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}