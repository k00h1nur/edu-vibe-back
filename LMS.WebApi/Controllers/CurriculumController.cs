using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Curriculum;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

/// <summary>
/// Curriculum template engine — list/read reusable learning paths, bind a path
/// to a class (auto-mapping its scheduled sessions to lessons), and read a
/// class's dated curriculum (the "today's topic" + planner source).
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CurriculumController(ISender sender) : ControllerBase
{
    /// <summary>All published templates (optionally filtered by category) with module/unit/lesson counts.</summary>
    [HttpGet("templates")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<CurriculumTemplateSummaryDto>>>> Templates(
        [FromQuery] LMS.Domain.Entities.CurriculumCategory? category, CancellationToken ct)
    {
        var r = await sender.Send(new GetCurriculumTemplatesQuery(category), ct);
        return Ok(ApiResponse<IReadOnlyCollection<CurriculumTemplateSummaryDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>The full Module → Unit → Lesson tree for one template.</summary>
    [HttpGet("templates/{id:guid}")]
    public async Task<ActionResult<ApiResponse<CurriculumTreeDto>>> Tree(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetCurriculumTreeQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<CurriculumTreeDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<CurriculumTreeDto>.Fail(r.Message ?? "Not found"));
    }

    /// <summary>
    /// Bind a template to a class and auto-map its upcoming sessions to the
    /// template's lessons. Body: {"classId": "...", "templateId": "..."}.
    /// </summary>
    [HttpPost("assign")]
    [PermissionAuthorize(Permissions.Classes.Update)]
    public async Task<ActionResult<ApiResponse<ClassCurriculumDto>>> Assign(
        [FromBody] AssignCurriculumToClassCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<ClassCurriculumDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<ClassCurriculumDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>A class's dated curriculum: progress + today + next + the full plan.</summary>
    [HttpGet("class/{classId:guid}")]
    public async Task<ActionResult<ApiResponse<ClassCurriculumDto>>> ForClass(Guid classId, CancellationToken ct)
    {
        var r = await sender.Send(new GetClassCurriculumQuery(classId), ct);
        return r.Success
            ? Ok(ApiResponse<ClassCurriculumDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<ClassCurriculumDto>.Fail(r.Message ?? "Not found"));
    }
}
