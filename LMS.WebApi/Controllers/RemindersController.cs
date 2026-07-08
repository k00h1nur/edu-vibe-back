using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Reminders;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class RemindersController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>Returns the caller's reminders. Self-only — no cross-user lookup.</summary>
    [HttpGet]
    [PermissionAuthorize(Permissions.Reminders.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<ReminderDto>>>> Mine(
        [FromQuery] bool includeCompleted = true, CancellationToken ct = default)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var r = await sender.Send(new GetMyRemindersQuery(currentUser.UserId.Value, includeCompleted), ct);
        return Ok(ApiResponse<IReadOnlyCollection<ReminderDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Reminders.Manage)]
    public async Task<ActionResult<ApiResponse<ReminderDto>>> Create(
        [FromBody] CreateReminderCommand cmd, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        // Force the owner to the caller — ignore any body field. Self-only.
        var r = await sender.Send(cmd with { OwnerUserId = currentUser.UserId.Value }, ct);
        return r.ToApiResult();
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Reminders.Manage)]
    public async Task<ActionResult<ApiResponse<ReminderDto>>> Update(
        Guid id, [FromBody] UpdateReminderCommand cmd, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var r = await sender.Send(cmd with { ReminderId = id, OwnerUserId = currentUser.UserId.Value }, ct);
        return r.ToApiResult();
    }

    [HttpPost("{id:guid}/complete")]
    [PermissionAuthorize(Permissions.Reminders.Manage)]
    public async Task<ActionResult<ApiResponse<ReminderDto>>> SetCompleted(
        Guid id, [FromBody] SetReminderCompletedCommand cmd, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var r = await sender.Send(cmd with { ReminderId = id, OwnerUserId = currentUser.UserId.Value }, ct);
        return r.ToApiResult();
    }

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(Permissions.Reminders.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var r = await sender.Send(new DeleteReminderCommand(id, currentUser.UserId.Value), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}
