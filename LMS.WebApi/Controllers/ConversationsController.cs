using LMS.Application.Features.Conversations;
using LMS.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class ConversationsController(ISender sender) : ControllerBase
{
    [HttpGet("my/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ConversationDto>>>> My(Guid userId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetMyConversationsQuery(userId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<ConversationDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ConversationDto>>> Create([FromBody] CreateConversationCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<ConversationDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<ConversationDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/participants/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Add(Guid id, Guid userId, CancellationToken ct)
    {
        var r = await sender.Send(new AddConversationParticipantCommand(id, userId), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}/participants/{userId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Remove(Guid id, Guid userId, CancellationToken ct)
    {
        var r = await sender.Send(new RemoveConversationParticipantCommand(id, userId), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}