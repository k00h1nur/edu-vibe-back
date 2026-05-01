using LMS.Application.Features.Submissions;
using LMS.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class SubmissionsController(ISender sender) : ControllerBase
{
    [HttpGet("assignment/{assignmentId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SubmissionDto>>>> Assignment(Guid assignmentId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetAssignmentSubmissionsQuery(assignmentId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<SubmissionDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("student/{studentProfileId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SubmissionDto>>>> Student(Guid studentProfileId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentSubmissionsQuery(studentProfileId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<SubmissionDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SubmissionDto>>> Submit([FromBody] SubmitAssignmentCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<SubmissionDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<SubmissionDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/grade/{score:decimal}")]
    public async Task<ActionResult<ApiResponse<SubmissionDto>>> Grade(Guid id, decimal score, CancellationToken ct)
    {
        var r = await sender.Send(new GradeSubmissionCommand(id, score), ct);
        return r.Success
            ? Ok(ApiResponse<SubmissionDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<SubmissionDto>.Fail(r.Message ?? "Failed"));
    }
}