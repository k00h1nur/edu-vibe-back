using LMS.Application.Features.Users;
using LMS.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class UsersController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<UserDto>>>> GetAll(CancellationToken ct)
    {
        var r = await sender.Send(new GetUsersQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<UserDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetById(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetUserByIdQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<UserDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<UserDto>.Fail(r.Message ?? "Not found"));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDto>>> Create([FromBody] CreateUserCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<UserDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<UserDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDto>>> Update(Guid id, [FromBody] UpdateUserCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { UserId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<UserDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<UserDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Deactivate(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new DeactivateUserCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}