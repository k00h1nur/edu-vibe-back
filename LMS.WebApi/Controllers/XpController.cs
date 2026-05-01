using LMS.Application.Features.Xp;
using LMS.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class XpController(ISender sender) : ControllerBase
{
    [HttpGet("student/{studentProfileId:guid}/ledger")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<XpLedgerDto>>>> Ledger(Guid studentProfileId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentXpLedgerQuery(studentProfileId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<XpLedgerDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("leaderboard")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<LeaderboardDto>>>> Leaderboard(
        [FromQuery] int top = 10, CancellationToken ct = default)
    {
        var r = await sender.Send(new GetLeaderboardQuery(top), ct);
        return Ok(ApiResponse<IReadOnlyCollection<LeaderboardDto>>.Ok(r.Data, r.Message));
    }

    [HttpPost("manual")]
    public async Task<ActionResult<ApiResponse<object>>> Manual([FromBody] AddManualXpCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<object>.Ok(new { }, r.Message))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }
}