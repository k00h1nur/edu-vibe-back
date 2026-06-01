using LMS.Application.Common.Security;
using LMS.Application.Features.TaskSubmissions;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TaskSubmissionsController(ISender sender) : ControllerBase
{
    /// <summary>Student submits a response to a task. Auto-grades closed-form types.</summary>
    [HttpPost]
    [PermissionAuthorize(Permissions.TaskSubmissions.Submit)]
    public async Task<ActionResult<ApiResponse<TaskSubmissionDto>>> Submit(
        [FromBody] SubmitTaskResponseCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<TaskSubmissionDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<TaskSubmissionDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>Teacher manually grades a submission (open-ended types).</summary>
    [HttpPost("{id:guid}/grade")]
    [PermissionAuthorize(Permissions.TaskSubmissions.Grade)]
    public async Task<ActionResult<ApiResponse<TaskSubmissionDto>>> Grade(Guid id,
        [FromBody] GradeTaskSubmissionCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { SubmissionId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<TaskSubmissionDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<TaskSubmissionDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>All submissions for a task — teacher view.</summary>
    [HttpGet("task/{taskId:guid}")]
    [PermissionAuthorize(Permissions.TaskSubmissions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TaskSubmissionDto>>>> ByTask(
        Guid taskId, CancellationToken ct)
    {
        var r = await sender.Send(new GetTaskSubmissionsByTaskQuery(taskId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<TaskSubmissionDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>The caller's own submissions for an assignment — student view.</summary>
    [HttpGet("assignment/{assignmentId:guid}/student/{studentProfileId:guid}")]
    [PermissionAuthorize(Permissions.TaskSubmissions.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<TaskSubmissionDto>>>> ByAssignmentForStudent(
        Guid assignmentId, Guid studentProfileId, CancellationToken ct)
    {
        var r = await sender.Send(
            new GetMyTaskSubmissionsByAssignmentQuery(assignmentId, studentProfileId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<TaskSubmissionDto>>.Ok(r.Data, r.Message));
    }
}
