using LMS.Application.Common.Security;
using LMS.Application.Features.Auth;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [HttpGet("ping")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<string>>> Ping(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new AuthPingCommand(), cancellationToken);
        return Ok(ApiResponse<string>.Ok(result.Data, result.Message));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthTokensResponse>>> Register([FromBody] RegisterUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.Success)
            return BadRequest(ApiResponse<AuthTokensResponse>.Fail(result.Message ?? "Register failed",
                result.ValidationErrors));
        return Ok(ApiResponse<AuthTokensResponse>.Ok(result.Data, "Registered"));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthTokensResponse>>> Login([FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.Success)
            return Unauthorized(ApiResponse<AuthTokensResponse>.Fail(result.Message ?? "Login failed"));
        return Ok(ApiResponse<AuthTokensResponse>.Ok(result.Data, "Logged in"));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<AuthTokensResponse>>> Refresh([FromBody] RefreshTokenCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.Success)
            return Unauthorized(ApiResponse<AuthTokensResponse>.Fail(result.Message ?? "Refresh failed"));
        return Ok(ApiResponse<AuthTokensResponse>.Ok(result.Data, "Token refreshed"));
    }

    /// <summary>
    /// Grants a role to a user. The handler enforces additional rules: only a
    /// SuperAdmin may grant SuperAdmin, and you can't change your own roles.
    /// </summary>
    [HttpPost("assign-role")]
    [PermissionAuthorize(Permissions.Auth.AssignRole)]
    public async Task<ActionResult<ApiResponse<object>>> AssignRole([FromBody] AssignRoleCommand command,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.Success) return BadRequest(ApiResponse<object>.Fail(result.Message ?? "Assign role failed"));
        return Ok(ApiResponse<object>.Ok(new { }, result.Message));
    }
}
