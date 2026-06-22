using LMS.Application.Features.Notifications;
using LMS.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

/// <summary>
/// In-app notification feed for the signed-in user. Self-scoped in the handler
/// (it reads only the caller's unread messages + the shared inquiry inbox for
/// staff), so no extra permission gate beyond authentication.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class NotificationsController(ISender sender) : ControllerBase
{
    /// <summary>Recent notifications, newest first. <paramref name="take"/> is clamped server-side.</summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<NotificationDto>>>> Get(
        [FromQuery] int take = 15, CancellationToken ct = default)
    {
        var r = await sender.Send(new GetNotificationsQuery(take), ct);
        if (r.Success)
            return Ok(ApiResponse<IReadOnlyCollection<NotificationDto>>.Ok(r.Data, r.Message));
        return StatusCode(StatusCodes.Status403Forbidden,
            ApiResponse<IReadOnlyCollection<NotificationDto>>.Fail(r.Message ?? "Forbidden"));
    }
}
