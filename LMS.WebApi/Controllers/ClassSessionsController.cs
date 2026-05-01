using LMS.Application.Features.Sessions;
using LMS.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ClassSessionsController(ISender sender) : ControllerBase
{
    [HttpGet("class/{classId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SessionDto>>>> ByClass(Guid classId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetClassSessionsQuery(classId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<SessionDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("my/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<SessionDto>>>> My(Guid userId, CancellationToken ct)
    {
        var r = await sender.Send(new GetMyScheduleQuery(userId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<SessionDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SessionDto>>> Create([FromBody] CreateClassSessionCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<SessionDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<SessionDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<SessionDto>>> Update(Guid id, [FromBody] UpdateClassSessionCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { SessionId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<SessionDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<SessionDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new CancelClassSessionCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}