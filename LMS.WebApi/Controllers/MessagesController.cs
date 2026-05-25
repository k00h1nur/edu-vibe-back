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
public sealed class MessagesController(ISender sender) : ControllerBase
{
    [HttpGet("conversation/{conversationId:guid}")]
    [PermissionAuthorize(Permissions.Messages.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<MessageDto>>>> Conversation(Guid conversationId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetConversationMessagesQuery(conversationId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<MessageDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Messages.Send)]
    public async Task<ActionResult<ApiResponse<MessageDto>>> Send([FromBody] SendMessageCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<MessageDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<MessageDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/read")]
    [PermissionAuthorize(Permissions.Messages.Read)]
    public async Task<ActionResult<ApiResponse<MessageDto>>> Read(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new MarkMessageAsReadCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<MessageDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<MessageDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>Number of unread messages addressed to the user across all their conversations.</summary>
    [HttpGet("unread-count/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<int>>> UnreadCount(Guid userId, CancellationToken ct)
    {
        var r = await sender.Send(new GetUnreadMessageCountQuery(userId), ct);
        return Ok(ApiResponse<int>.Ok(r.Data, r.Message));
    }
}
