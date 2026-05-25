using LMS.Application.Common.Security;
using LMS.Application.Features.Submissions;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
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
    [PermissionAuthorize(Permissions.Submissions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SubmissionDto>>>> Assignment(Guid assignmentId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetAssignmentSubmissionsQuery(assignmentId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<SubmissionDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("student/{studentProfileId:guid}")]
    [PermissionAuthorize(Permissions.Submissions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SubmissionDto>>>> Student(Guid studentProfileId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentSubmissionsQuery(studentProfileId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<SubmissionDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Submissions.Create)]
    public async Task<ActionResult<ApiResponse<SubmissionDto>>> Submit([FromBody] SubmitAssignmentCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<SubmissionDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<SubmissionDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/grade/{score:decimal}")]
    [PermissionAuthorize(Permissions.Submissions.Grade)]
    public async Task<ActionResult<ApiResponse<SubmissionDto>>> Grade(Guid id, decimal score, CancellationToken ct)
    {
        var r = await sender.Send(new GradeSubmissionCommand(id, score), ct);
        return r.Success
            ? Ok(ApiResponse<SubmissionDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<SubmissionDto>.Fail(r.Message ?? "Failed"));
    }
}
