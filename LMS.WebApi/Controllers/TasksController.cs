using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Security;
using LMS.Application.Features.Tasks;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class TasksController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Lists tasks under an assignment. Solutions are stripped from the
    /// response for non-staff callers — only the allowlist roles see them.
    /// </summary>
    [HttpGet("assignment/{assignmentId:guid}")]
    [PermissionAuthorize(Permissions.Tasks.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LearningTaskDto>>>> GetByAssignment(
        Guid assignmentId, CancellationToken ct)
    {
        var r = await sender.Send(new GetAssignmentTasksQuery(assignmentId, CallerCanSeeSolutions()), ct);
        return Ok(ApiResponse<IReadOnlyCollection<LearningTaskDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}")]
    [PermissionAuthorize(Permissions.Tasks.Read)]
    public async Task<ActionResult<ApiResponse<LearningTaskDto>>> Get(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetTaskByIdQuery(id, CallerCanSeeSolutions()), ct);
        return r.ToApiResultOrNotFound();
    }

    /// <summary>
    /// True only when the caller is a staff member (see <see cref="RoleGroups.Staff"/>)
    /// allowed to view answer keys. Allowlist (not denylist) so a newly-introduced
    /// role (Parent, Observer, etc.) cannot accidentally leak solutions to students.
    /// </summary>
    private bool CallerCanSeeSolutions() => currentUser.IsStaff();

    [HttpPost]
    [PermissionAuthorize(Permissions.Tasks.Manage)]
    public async Task<ActionResult<ApiResponse<LearningTaskDto>>> Create(
        [FromBody] CreateTaskCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.ToApiResult();
    }

    /// <summary>
    /// F4: materialise a lesson's default tasks into real LearningTasks under one
    /// assignment for the session. Idempotent — re-running adds only missing tasks.
    /// Self-scoped in the handler to the class teacher or an admin.
    /// </summary>
    [HttpPost("lesson-session/{sessionId:guid}/materialize")]
    [PermissionAuthorize(Permissions.Tasks.Manage)]
    public async Task<ActionResult<ApiResponse<MaterializeLessonTasksResultDto>>> Materialize(
        Guid sessionId, CancellationToken ct)
    {
        var r = await sender.Send(new MaterializeLessonTasksCommand(sessionId), ct);
        return r.Success
            ? Ok(ApiResponse<MaterializeLessonTasksResultDto>.Ok(r.Data, r.Message))
            : r.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(ApiResponse<MaterializeLessonTasksResultDto>.Fail(r.Message ?? "Not found")),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden,
                    ApiResponse<MaterializeLessonTasksResultDto>.Fail(r.Message ?? "Forbidden")),
                _ => BadRequest(ApiResponse<MaterializeLessonTasksResultDto>.Fail(r.Message ?? "Failed")),
            };
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Tasks.Manage)]
    public async Task<ActionResult<ApiResponse<LearningTaskDto>>> Update(Guid id,
        [FromBody] UpdateTaskCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { TaskId = id }, ct);
        return r.ToApiResult();
    }

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(Permissions.Tasks.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new DeleteTaskCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("assignment/{assignmentId:guid}/reorder")]
    [PermissionAuthorize(Permissions.Tasks.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> Reorder(Guid assignmentId,
        [FromBody] List<Guid> taskIds, CancellationToken ct)
    {
        var r = await sender.Send(new ReorderTasksCommand(assignmentId, taskIds), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}
