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

    /// <summary>Returns the currently authenticated user.</summary>
    [HttpGet("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMine(CancellationToken ct)
    {
        var r = await sender.Send(new GetMyUserQuery(), ct);
        return r.Success
            ? Ok(ApiResponse<UserDto>.Ok(r.Data, r.Message))
            : Unauthorized(ApiResponse<UserDto>.Fail(r.Message ?? "Unauthorized"));
    }

    /// <summary>Updates the currently authenticated user's profile.</summary>
    [HttpPut("me")]
    public async Task<ActionResult<ApiResponse<UserDto>>> UpdateMine(
        [FromBody] UpdateMyUserCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<UserDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<UserDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>Lets the authenticated user change their own password.</summary>
    [HttpPost("me/password")]
    public async Task<ActionResult<ApiResponse<object>>> ChangeMyPassword(
        [FromBody] ChangeMyPasswordCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
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