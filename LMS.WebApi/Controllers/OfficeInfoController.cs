using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.OfficeInfo;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OfficeInfoController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Reads the singleton office info. Open to anonymous callers — the
    /// marketing site reads this without auth to render the public site.
    /// Private-only fields (none today) would need a separate gated endpoint.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<OfficeInfoDto>>> Get(CancellationToken ct)
    {
        var r = await sender.Send(new GetOfficeInfoQuery(), ct);
        return Ok(ApiResponse<OfficeInfoDto>.Ok(r.Data, r.Message));
    }

    [HttpPut]
    [Authorize]
    [PermissionAuthorize(Permissions.OfficeInfoPermissions.Manage)]
    public async Task<ActionResult<ApiResponse<OfficeInfoDto>>> Update(
        [FromBody] UpdateOfficeInfoCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<OfficeInfoDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<OfficeInfoDto>.Fail(r.Message ?? "Failed"));
    }
}
