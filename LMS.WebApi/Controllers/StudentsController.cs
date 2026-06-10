using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Students;
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
public sealed class StudentsController(ISender sender) : ControllerBase
{
    /// <summary>Lists student profiles, paginated. Admin/teacher only.</summary>
    [HttpGet]
    [PermissionAuthorize(Permissions.Students.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<StudentDto>>>> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var r = await sender.Send(new GetStudentsQuery(page, pageSize, search), ct);
        return Ok(ApiResponse<PagedResult<StudentDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>Returns the student profile linked to the authenticated user. No extra permission required.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> GetMine(CancellationToken ct)
    {
        var r = await sender.Send(new GetMyStudentProfileQuery(), ct);
        return r.Success
            ? Ok(ApiResponse<StudentDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<StudentDto>.Fail(r.Message ?? "Not found"));
    }

    [HttpGet("{id:guid}")]
    [PermissionAuthorize(Permissions.Students.Read)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> Get(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentDetailQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<StudentDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<StudentDto>.Fail(r.Message ?? "Not found"));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Students.Create)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> Register([FromBody] RegisterStudentCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<StudentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<StudentDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Students.Update)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> Update(Guid id, [FromBody] UpdateStudentProfileCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { StudentProfileId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<StudentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<StudentDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>
    /// Updates the editable profile fields — first name, last name, phone
    /// number, description. Separate from <see cref="Update"/> which handles
    /// XP / streak.
    /// </summary>
    [HttpPut("{id:guid}/details")]
    [PermissionAuthorize(Permissions.Students.Update)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> UpdateDetails(
        Guid id, [FromBody] UpdateStudentDetailsCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { StudentProfileId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<StudentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<StudentDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>Admin-only fields: parent phone number, CEFR level.</summary>
    [HttpPut("{id:guid}/admin-fields")]
    [PermissionAuthorize(Permissions.Students.Update)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> UpdateAdminFields(
        Guid id, [FromBody] UpdateStudentAdminFieldsCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { StudentProfileId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<StudentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<StudentDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>
    /// Admin freeze/block/restore. See StaffController.SetStatus for semantics —
    /// flipping out of Active also invalidates the student's refresh token so
    /// existing sessions can't survive past the next access-token refresh.
    /// </summary>
    [HttpPost("{id:guid}/status")]
    [PermissionAuthorize(Permissions.Students.Update)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> SetStatus(Guid id,
        [FromBody] SetUserStatusRequest body, CancellationToken ct)
    {
        var r = await sender.Send(new SetStudentStatusCommand(id, body.Status), ct);
        return r.Success
            ? Ok(ApiResponse<StudentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<StudentDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>Multipart avatar upload — students or admins.</summary>
    [HttpPost("{id:guid}/avatar")]
    [PermissionAuthorize(Permissions.Students.Update)]
    [RequestSizeLimit(2 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> UploadAvatar(Guid id,
        IFormFile file,
        [FromServices] LMS.Application.Common.Abstractions.IAvatarFileStore store,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<StudentDto>.Fail("File is required."));

        string storedName;
        try
        {
            await using var stream = file.OpenReadStream();
            storedName = await store.SaveAsync(stream, file.FileName, file.ContentType, ct);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<StudentDto>.Fail(ex.Message));
        }

        var r = await sender.Send(new SetStudentAvatarCommand(id, $"/uploads/avatars/{storedName}"), ct);
        if (!r.Success)
        {
            await store.DeleteAsync(storedName, ct);
            return BadRequest(ApiResponse<StudentDto>.Fail(r.Message ?? "Failed"));
        }
        return Ok(ApiResponse<StudentDto>.Ok(r.Data, r.Message));
    }
}
