using LMS.Application.Features.Students;
using LMS.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class StudentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<StudentDto>>>> GetAll(CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentsQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<StudentDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> Get(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentDetailQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<StudentDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<StudentDto>.Fail(r.Message ?? "Not found"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<StudentDto>>> Register([FromBody] RegisterStudentCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<StudentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<StudentDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<StudentDto>>> Update(Guid id, [FromBody] UpdateStudentProfileCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { StudentProfileId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<StudentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<StudentDto>.Fail(r.Message ?? "Failed"));
    }
}