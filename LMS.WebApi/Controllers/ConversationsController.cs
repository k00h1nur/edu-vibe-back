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

    /// <summary>
    /// People the caller may start a conversation with, scoped by role: a student
    /// sees classmates + the teachers of their classes + admins; a teacher sees
    /// students in classes they teach + admins; an admin sees everyone.
    /// </summary>
    [HttpGet("contacts")]
    [PermissionAuthorize(Permissions.Conversations.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ContactDto>>>> Contacts(CancellationToken ct)
    {
        var r = await sender.Send(new GetMessageableContactsQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<ContactDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Conversations.Create)]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> Create([FromBody] CreateConversationCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.ToApiResult();
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
