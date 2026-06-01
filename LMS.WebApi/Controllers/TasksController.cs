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
    /// response for student callers — only Tasks.Manage holders see them.
    /// </summary>
    [HttpGet("assignment/{assignmentId:guid}")]
    [PermissionAuthorize(Permissions.Tasks.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LearningTaskDto>>>> GetByAssignment(
        Guid assignmentId, CancellationToken ct)
    {
        var canSeeSolutions = currentUser.Roles.Any(r =>
            string.Equals(r, RoleCodes.SuperAdmin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, RoleCodes.Admin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, RoleCodes.AcademyDirector, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, RoleCodes.OfficeAdmin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, RoleCodes.Teacher, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, RoleCodes.SupportTeacher, StringComparison.OrdinalIgnoreCase));

        var r = await sender.Send(new GetAssignmentTasksQuery(assignmentId, canSeeSolutions), ct);
        return Ok(ApiResponse<IReadOnlyCollection<LearningTaskDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}")]
    [PermissionAuthorize(Permissions.Tasks.Read)]
    public async Task<ActionResult<ApiResponse<LearningTaskDto>>> Get(Guid id, CancellationToken ct)
    {
        var canSeeSolutions = currentUser.Roles.Any(r =>
            !string.Equals(r, RoleCodes.Student, StringComparison.OrdinalIgnoreCase));
        var r = await sender.Send(new GetTaskByIdQuery(id, canSeeSolutions), ct);
        return r.Success
            ? Ok(ApiResponse<LearningTaskDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<LearningTaskDto>.Fail(r.Message ?? "Not found"));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Tasks.Manage)]
    public async Task<ActionResult<ApiResponse<LearningTaskDto>>> Create(
        [FromBody] CreateTaskCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<LearningTaskDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<LearningTaskDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Tasks.Manage)]
    public async Task<ActionResult<ApiResponse<LearningTaskDto>>> Update(Guid id,
        [FromBody] UpdateTaskCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { TaskId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<LearningTaskDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<LearningTaskDto>.Fail(r.Message ?? "Failed"));
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
