using LMS.Application.Common.Security;
using LMS.Application.Features.Auth;
using LMS.Application.Features.Telegram;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace LMS.WebApi.Controllers;

/// <summary>
/// Telegram Mini App auth surface. <c>/auth</c> is the only anonymous endpoint —
/// it trades a signed initData for the same JWT/refresh pair as email login.
/// The link/profile/unlink endpoints operate on the authenticated web user so a
/// signed-in person can connect or disconnect their Telegram from the panels.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class TelegramController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Authenticate with <c>window.Telegram.WebApp.initData</c>. Signs in the
    /// linked user or auto-provisions a Student on first contact. Anonymous +
    /// rate-limited (shares the auth throttle) since it mints a session.
    /// </summary>
    [HttpPost("auth")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-anon")]
    public async Task<ActionResult<ApiResponse<AuthTokensResponse>>> Auth(
        [FromBody] TelegramAuthCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.Success)
            return Unauthorized(ApiResponse<AuthTokensResponse>.Fail(result.Message ?? "Telegram auth failed"));
        return Ok(ApiResponse<AuthTokensResponse>.Ok(result.Data, result.Message));
    }

    /// <summary>Connect the verified Telegram identity to the signed-in user.</summary>
    [HttpPost("link")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TelegramProfileDto>>> Link(
        [FromBody] TelegramLinkCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.Success)
            return BadRequest(ApiResponse<TelegramProfileDto>.Fail(result.Message ?? "Telegram link failed"));
        return Ok(ApiResponse<TelegramProfileDto>.Ok(result.Data, result.Message));
    }

    /// <summary>The signed-in user's linked Telegram profile (null if none).</summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TelegramProfileDto?>>> Profile(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTelegramProfileQuery(), cancellationToken);
        if (!result.Success)
            return BadRequest(ApiResponse<TelegramProfileDto?>.Fail(result.Message ?? "Failed to load profile"));
        return Ok(ApiResponse<TelegramProfileDto?>.Ok(result.Data, result.Message));
    }

    /// <summary>Disconnect the signed-in user's Telegram link (idempotent).</summary>
    [HttpDelete("link")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Unlink(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnlinkTelegramCommand(), cancellationToken);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Fail(result.Message ?? "Telegram unlink failed"));
        return Ok(ApiResponse<object>.Ok(new { }, result.Message));
    }

    /// <summary>
    /// Mints a one-time deep-link handoff token for the signed-in user and
    /// returns the Telegram Mini App deep link to open. The Mini App then signs
    /// the same user in (no password) via the token.
    /// </summary>
    [HttpPost("deep-link")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DeepLinkTokenDto>>> CreateDeepLink(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateDeepLinkTokenCommand(), cancellationToken);
        if (!result.Success)
            return BadRequest(ApiResponse<DeepLinkTokenDto>.Fail(result.Message ?? "Couldn't create deep link"));
        return Ok(ApiResponse<DeepLinkTokenDto>.Ok(result.Data, result.Message));
    }

    // ----- Platform bot settings ------------------------------------------

    /// <summary>
    /// Public bot settings (just the @username) so any panel can build the
    /// "Open in Telegram" deep link. Anonymous — the username isn't a secret.
    /// </summary>
    [HttpGet("settings")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TelegramSettingsDto>>> GetSettings(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTelegramSettingsQuery(), cancellationToken);
        return Ok(ApiResponse<TelegramSettingsDto>.Ok(result.Data, result.Message));
    }

    /// <summary>Admin upsert of the bot @username. Reuses the OfficeInfo.Manage permission.</summary>
    [HttpPut("settings")]
    [Authorize]
    [PermissionAuthorize(Permissions.OfficeInfo.Manage)]
    public async Task<ActionResult<ApiResponse<TelegramSettingsDto>>> UpdateSettings(
        [FromBody] UpdateTelegramSettingsCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.Success)
            return BadRequest(ApiResponse<TelegramSettingsDto>.Fail(result.Message ?? "Failed to save settings"));
        return Ok(ApiResponse<TelegramSettingsDto>.Ok(result.Data, result.Message));
    }
}
