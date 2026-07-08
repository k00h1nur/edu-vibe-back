using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Security;
using LMS.Application.Features.Messages;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class MessagesController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Returns a page of messages from a conversation. Default page size is 50;
    /// pass <paramref name="before"/> = oldest message's CreatedAt to walk back
    /// through history page-by-page. The caller must be a participant — the
    /// handler returns 403 otherwise.
    /// </summary>
    [HttpGet("conversation/{conversationId:guid}")]
    [PermissionAuthorize(Permissions.Messages.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MessageDto>>>> Conversation(
        Guid conversationId,
        [FromQuery] DateTime? before,
        [FromQuery] int limit = 50,
        CancellationToken ct = default)
    {
        var r = await sender.Send(
            new GetConversationMessagesQuery(conversationId, before, limit), ct);
        return r.Success
            ? Ok(ApiResponse<IReadOnlyCollection<MessageDto>>.Ok(r.Data, r.Message))
            : StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<IReadOnlyCollection<MessageDto>>.Fail(r.Message ?? "Forbidden"));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Messages.Send)]
    public async Task<ActionResult<ApiResponse<MessageDto>>> Send([FromBody] SendMessageCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.ToApiResult();
    }

    [HttpPost("{id:guid}/read")]
    [PermissionAuthorize(Permissions.Messages.Read)]
    public async Task<ActionResult<ApiResponse<MessageDto>>> Read(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new MarkMessageAsReadCommand(id), ct);
        return r.ToApiResult();
    }

    /// <summary>
    /// Number of unread messages addressed to the caller across all their conversations.
    /// Self-only — the route id must match the authenticated user, otherwise 403.
    /// (Kept as a route parameter for backwards-compat with existing clients;
    /// new clients should ignore the value and rely on the auth context.)
    /// </summary>
    [HttpGet("unread-count/{userId:guid}")]
    [PermissionAuthorize(Permissions.Messages.Read)]
    public async Task<ActionResult<ApiResponse<int>>> UnreadCount(Guid userId, CancellationToken ct)
    {
        if (currentUser.UserId is null || currentUser.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<int>.Fail("Unread count is self-only."));

        var r = await sender.Send(new GetUnreadMessageCountQuery(userId), ct);
        return Ok(ApiResponse<int>.Ok(r.Data, r.Message));
    }
}
