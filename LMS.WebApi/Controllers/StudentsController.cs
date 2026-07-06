using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Analytics;
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
        return r.ToApiResultOrNotFound();
    }

    /// <summary>
    /// A student's measurable performance summary (attendance %, assignment +
    /// lesson completion, average score, missing/late counts). Self-scoped in
    /// the handler — the student themselves, a teacher of their class, or staff.
    /// No permission gate (students lack Analytics.Read but need their own).
    /// </summary>
    [HttpGet("{id:guid}/performance")]
    public async Task<ActionResult<ApiResponse<StudentPerformanceDto>>> Performance(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentPerformanceQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<StudentPerformanceDto>.Ok(r.Data, r.Message))
            : r.ErrorCode == "FORBIDDEN"
                ? StatusCode(StatusCodes.Status403Forbidden, ApiResponse<StudentPerformanceDto>.Fail(r.Message ?? "Forbidden"))
                : NotFound(ApiResponse<StudentPerformanceDto>.Fail(r.Message ?? "Not found"));
    }

    /// <summary>
    /// Self-edit: a student updates their own profile (name, phone, about).
    /// The target profile is resolved from the JWT — body carries no
    /// profile id, so there's no IDOR surface. Admin-only fields
    /// (parent phone, CEFR level, XP, streak) stay untouched.
    /// </summary>
    [HttpPut("me/details")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> UpdateMine(
        [FromBody] UpdateMyStudentDetailsCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.ToApiResult();
    }

    [HttpGet("{id:guid}")]
    [PermissionAuthorize(Permissions.Students.Read)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> Get(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentDetailQuery(id), ct);
        return r.ToApiResultOrNotFound();
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Students.Create)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> Register([FromBody] RegisterStudentCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.ToApiResult();
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Students.Update)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> Update(Guid id, [FromBody] UpdateStudentProfileCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { StudentProfileId = id }, ct);
        return r.ToApiResult();
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
        return r.ToApiResult();
    }

    /// <summary>Admin-only fields: parent phone number, CEFR level.</summary>
    [HttpPut("{id:guid}/admin-fields")]
    [PermissionAuthorize(Permissions.Students.Update)]
    public async Task<ActionResult<ApiResponse<StudentDto>>> UpdateAdminFields(
        Guid id, [FromBody] UpdateStudentAdminFieldsCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { StudentProfileId = id }, ct);
        return r.ToApiResult();
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
        return r.ToApiResult();
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

    /// <summary>
    /// Self-service avatar upload — a student changes their own photo from
    /// Settings. The profile is resolved from the JWT (no id in the route),
    /// so plain [Authorize] is enough; the Students.Update-gated {id}/avatar
    /// endpoint above stays for admins changing someone else's photo.
    /// </summary>
    [HttpPost("me/avatar")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> UploadMyAvatar(
        IFormFile file,
        [FromServices] LMS.Application.Common.Abstractions.IAvatarFileStore store,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(ApiResponse<StudentDto>.Fail("File is required."));

        var mine = await sender.Send(new GetMyStudentProfileQuery(), ct);
        if (!mine.Success || mine.Data is null)
            return NotFound(ApiResponse<StudentDto>.Fail(mine.Message ?? "No student profile for this user."));

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

        var r = await sender.Send(new SetStudentAvatarCommand(mine.Data.StudentProfileId, $"/uploads/avatars/{storedName}"), ct);
        if (!r.Success)
        {
            await store.DeleteAsync(storedName, ct);
            return BadRequest(ApiResponse<StudentDto>.Fail(r.Message ?? "Failed"));
        }
        return Ok(ApiResponse<StudentDto>.Ok(r.Data, r.Message));
    }
}
