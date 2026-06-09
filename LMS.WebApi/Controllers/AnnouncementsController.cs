using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Announcements;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AnnouncementsController(ISender sender, ICurrentUserService currentUser) : ControllerBase
{
    /// <summary>
    /// Lists announcements. Non-managers see published-only; managers see
    /// everything (drafts visible for editing).
    /// </summary>
    [HttpGet]
    [PermissionAuthorize(Permissions.Announcements.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<AnnouncementDto>>>> List(
        [FromQuery] bool? includeDrafts, CancellationToken ct)
    {
        var canManage = User.HasClaim("permission", Permissions.Announcements.Manage);
        var publishedOnly = !(canManage && includeDrafts == true);
        var r = await sender.Send(new GetAnnouncementsQuery(publishedOnly), ct);
        return Ok(ApiResponse<IReadOnlyCollection<AnnouncementDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Announcements.Manage)]
    public async Task<ActionResult<ApiResponse<AnnouncementDto>>> Create(
        [FromBody] CreateAnnouncementCommand cmd, CancellationToken ct)
    {
        if (currentUser.UserId is null) return Unauthorized();
        var r = await sender.Send(cmd with { AuthorUserId = currentUser.UserId.Value }, ct);
        return r.Success
            ? Ok(ApiResponse<AnnouncementDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AnnouncementDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Announcements.Manage)]
    public async Task<ActionResult<ApiResponse<AnnouncementDto>>> Update(
        Guid id, [FromBody] UpdateAnnouncementCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { AnnouncementId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<AnnouncementDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AnnouncementDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/publish")]
    [PermissionAuthorize(Permissions.Announcements.Manage)]
    public async Task<ActionResult<ApiResponse<AnnouncementDto>>> SetPublished(
        Guid id, [FromBody] SetAnnouncementPublishedCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { AnnouncementId = id }, ct);
        return r.Success
            ? Ok(ApiResponse<AnnouncementDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<AnnouncementDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(Permissions.Announcements.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new DeleteAnnouncementCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}
