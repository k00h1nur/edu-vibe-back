using LMS.Application.Common.Security;
using LMS.Application.Features.VisitorMessages;
using LMS.Domain.Entities;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

/// <summary>
/// Inbox for messages left by anonymous visitors via the marketing site
/// (contact form + demo lesson request). Admin endpoints require
/// <see cref="Permissions.VisitorMessages.Read"/> / <see cref="Permissions.VisitorMessages.Update"/>.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class VisitorMessagesController(ISender sender) : ControllerBase
{
    /// <summary>Submit a message as an unauthenticated visitor. Triggers a Telegram ping.</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<VisitorMessageDto>>> Submit(
        [FromBody] CreateVisitorMessageCommand command, CancellationToken ct)
    {
        var r = await sender.Send(command, ct);
        return r.Success
            ? Ok(ApiResponse<VisitorMessageDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<VisitorMessageDto>.Fail(r.Message ?? "Failed", r.ValidationErrors));
    }

    /// <summary>Paginated admin inbox.</summary>
    [HttpGet]
    [Authorize]
    [PermissionAuthorize(Permissions.VisitorMessages.Read)]
    public async Task<ActionResult<ApiResponse<VisitorMessagePage>>> List(
        [FromQuery] bool? isRead,
        [FromQuery] VisitorMessageSource? source,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var r = await sender.Send(new GetVisitorMessagesQuery(isRead, source, page, pageSize), ct);
        return Ok(ApiResponse<VisitorMessagePage>.Ok(r.Data, r.Message));
    }

    /// <summary>Unread badge count for the admin top-bar.</summary>
    [HttpGet("unread-count")]
    [Authorize]
    [PermissionAuthorize(Permissions.VisitorMessages.Read)]
    public async Task<ActionResult<ApiResponse<int>>> UnreadCount(CancellationToken ct)
    {
        var r = await sender.Send(new GetUnreadVisitorMessageCountQuery(), ct);
        return Ok(ApiResponse<int>.Ok(r.Data, r.Message));
    }

    /// <summary>Toggle read state. Pass <c>?read=false</c> to mark unread.</summary>
    [HttpPost("{id:guid}/read")]
    [Authorize]
    [PermissionAuthorize(Permissions.VisitorMessages.Update)]
    public async Task<ActionResult<ApiResponse<VisitorMessageDto>>> SetRead(
        Guid id, [FromQuery] bool read = true, CancellationToken ct = default)
    {
        var r = await sender.Send(new MarkVisitorMessageReadCommand(id, read), ct);
        return r.Success
            ? Ok(ApiResponse<VisitorMessageDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<VisitorMessageDto>.Fail(r.Message ?? "Not found"));
    }
}
