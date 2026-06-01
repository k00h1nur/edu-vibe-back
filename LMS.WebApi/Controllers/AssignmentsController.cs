using LMS.Application.Common.Security;
using LMS.Application.Features.Assignments;
using LMS.Domain.Enums;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
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
    [PermissionAuthorize(Permissions.Assignments.Read)]
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
    [PermissionAuthorize(Permissions.Assignments.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssignmentDto>>>> Class(Guid classId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetClassAssignmentsQuery(classId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<AssignmentDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("student/{studentProfileId:guid}")]
    [PermissionAuthorize(Permissions.Assignments.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssignmentDto>>>> Student(Guid studentProfileId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentAssignmentsQuery(studentProfileId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<AssignmentDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Assignments.Create)]
    public async Task<ActionResult<ApiResponse<AssignmentDto>>> Create([FromBody] CreateAssignmentCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<AssignmentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AssignmentDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Assignments.Update)]
    public async Task<ActionResult<ApiResponse<AssignmentDto>>> Update(Guid id, [FromBody] UpdateAssignmentCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { AssignmentId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<AssignmentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AssignmentDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/publish")]
    [PermissionAuthorize(Permissions.Assignments.Publish)]
    public async Task<ActionResult<ApiResponse<AssignmentDto>>> Publish(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new PublishAssignmentCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<AssignmentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AssignmentDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/close")]
    [PermissionAuthorize(Permissions.Assignments.Close)]
    public async Task<ActionResult<ApiResponse<AssignmentDto>>> Close(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new CloseAssignmentCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<AssignmentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AssignmentDto>.Fail(r.Message ?? "Failed"));
    }

    // ----- Book attachments ------------------------------------------------

    /// <summary>Books attached to this assignment (reference material).</summary>
    [HttpGet("{id:guid}/books")]
    [PermissionAuthorize(Permissions.Assignments.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssignmentBookDto>>>> GetBooks(Guid id,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetAssignmentBooksQuery(id), ct);
        return Ok(ApiResponse<IReadOnlyCollection<AssignmentBookDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>Attach a book to this assignment. If already attached, updates the note.</summary>
    [HttpPost("{id:guid}/books")]
    [PermissionAuthorize(Permissions.Assignments.Update)]
    public async Task<ActionResult<ApiResponse<AssignmentBookDto>>> AttachBook(Guid id,
        [FromBody] AttachBookToAssignmentCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { AssignmentId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<AssignmentBookDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AssignmentBookDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>Detach a book from this assignment.</summary>
    [HttpDelete("{id:guid}/books/{bookId:guid}")]
    [PermissionAuthorize(Permissions.Assignments.Update)]
    public async Task<ActionResult<ApiResponse<object>>> DetachBook(Guid id, Guid bookId, CancellationToken ct)
    {
        var r = await sender.Send(new DetachBookFromAssignmentCommand(id, bookId), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(null, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }

    // ----- Per-student targeting ------------------------------------------

    /// <summary>Targeted assignees. Empty list = assignment is visible to the whole class.</summary>
    [HttpGet("{id:guid}/assignees")]
    [PermissionAuthorize(Permissions.Assignments.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssignmentAssigneeDto>>>> GetAssignees(Guid id,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetAssignmentAssigneesQuery(id), ct);
        return Ok(ApiResponse<IReadOnlyCollection<AssignmentAssigneeDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>Replace the assignee set. Empty body = whole class (default). Body = subset of students.</summary>
    [HttpPut("{id:guid}/assignees")]
    [PermissionAuthorize(Permissions.Assignments.Update)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AssignmentAssigneeDto>>>> SetAssignees(Guid id,
        [FromBody] IReadOnlyCollection<Guid> studentProfileIds, CancellationToken ct)
    {
        var r = await sender.Send(new SetAssignmentAssigneesCommand(id, studentProfileIds ?? Array.Empty<Guid>()), ct);
        return r.Success
            ? Ok(ApiResponse<IReadOnlyCollection<AssignmentAssigneeDto>>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<IReadOnlyCollection<AssignmentAssigneeDto>>.Fail(r.Message ?? "Failed"));
    }
}
