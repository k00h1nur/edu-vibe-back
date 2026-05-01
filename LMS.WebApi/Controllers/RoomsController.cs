using LMS.Application.Features.Rooms;
using LMS.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RoomsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<RoomDto>>>> GetAll(CancellationToken ct)
    {
        var r = await sender.Send(new GetRoomsQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<RoomDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<RoomDto>>> Create([FromBody] CreateRoomCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<RoomDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<RoomDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<RoomDto>>> Update(Guid id, [FromBody] UpdateRoomCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { RoomId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<RoomDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<RoomDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new DeleteRoomCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}