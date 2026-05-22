using LMS.Application.Features.Assignments;
using LMS.Domain.Enums;
using LMS.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AssignmentsController(ISender sender) : ControllerBase
{
    /// <summary>Lists assignments, optionally filtered by teacher, class, or status.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssignmentDto>>>> GetAll(
        [FromQuery] Guid? teacherUserId,
        [FromQuery] Guid? classId,
        [FromQuery] AssignmentStatus? status,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetAssignmentsQuery(teacherUserId, classId, status), ct);
        return Ok(ApiResponse<IReadOnlyCollection<AssignmentDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("class/{classId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssignmentDto>>>> Class(Guid classId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetClassAssignmentsQuery(classId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<AssignmentDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("student/{studentProfileId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssignmentDto>>>> Student(Guid studentProfileId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentAssignmentsQuery(studentProfileId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<AssignmentDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AssignmentDto>>> Create([FromBody] CreateAssignmentCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<AssignmentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AssignmentDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssignmentDto>>> Update(Guid id, [FromBody] UpdateAssignmentCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { AssignmentId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<AssignmentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AssignmentDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<ActionResult<ApiResponse<AssignmentDto>>> Publish(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new PublishAssignmentCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<AssignmentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AssignmentDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/close")]
    public async Task<ActionResult<ApiResponse<AssignmentDto>>> Close(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new CloseAssignmentCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<AssignmentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AssignmentDto>.Fail(r.Message ?? "Failed"));
    }
}