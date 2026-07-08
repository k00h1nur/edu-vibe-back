using LMS.Application.Common.Security;
using LMS.Application.Features.Badges;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class BadgesController(ISender sender) : ControllerBase
{
    [HttpGet("student/{studentProfileId:guid}")]
    [PermissionAuthorize(Permissions.Badges.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<BadgeDto>>>> Student(Guid studentProfileId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentBadgesQuery(studentProfileId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<BadgeDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Badges.Create)]
    public async Task<ActionResult<ApiResponse<BadgeDto>>> Create([FromBody] CreateBadgeCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.ToApiResult();
    }

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Badges.Update)]
    public async Task<ActionResult<ApiResponse<BadgeDto>>> Update(Guid id, [FromBody] UpdateBadgeCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd with { BadgeId = id }, ct);
        return r.ToApiResult();
    }

    [HttpPost("{id:guid}/award/{studentProfileId:guid}")]
    [PermissionAuthorize(Permissions.Badges.Award)]
    public async Task<ActionResult<ApiResponse<object>>> Award(Guid id, Guid studentProfileId, CancellationToken ct)
    {
        var r = await sender.Send(new AwardBadgeCommand(id, studentProfileId), ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}
