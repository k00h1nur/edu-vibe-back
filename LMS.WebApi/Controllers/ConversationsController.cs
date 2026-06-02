using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Security;
using LMS.Application.Features.Conversations;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ConversationsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// The caller's own conversations. Self-only — the route id must match the
    /// authenticated user, otherwise 403. The userId in the route is kept for
    /// backwards-compat; new clients should ignore the value and rely on the
    /// auth context.
    /// </summary>
    [HttpGet("my/{userId:guid}")]
    [PermissionAuthorize(Permissions.Conversations.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ConversationDto>>>> My(Guid userId,
        CancellationToken ct)
    {
        if (currentUser.UserId is null || currentUser.UserId != userId)
            return StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<IReadOnlyCollection<ConversationDto>>.Fail("Conversation list is self-only."));

        var r = await sender.Send(new GetMyConversationsQuery(userId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<ConversationDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Conversations.Create)]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> Create([FromBody] CreateConversationCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<ConversationDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<ConversationDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/participants/{userId:guid}")]
    [PermissionAuthorize(Permissions.Conversations.ManageParticipants)]
    public async Task<ActionResult<ApiResponse<object>>> Add(Guid id, Guid userId, CancellationToken ct)
    {
        var r = await sender.Send(new AddConversationParticipantCommand(id, userId), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}/participants/{userId:guid}")]
    [PermissionAuthorize(Permissions.Conversations.ManageParticipants)]
    public async Task<ActionResult<ApiResponse<object>>> Remove(Guid id, Guid userId, CancellationToken ct)
    {
        var r = await sender.Send(new RemoveConversationParticipantCommand(id, userId), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}
