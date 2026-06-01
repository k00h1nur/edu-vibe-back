using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Students;
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
}
