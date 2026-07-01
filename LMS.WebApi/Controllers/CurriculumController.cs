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

    /// <summary>Admin: ALL master templates (published + unpublished) with counts + class usage.</summary>
    [HttpGet("admin/templates")]
    [PermissionAuthorize(Permissions.Classes.Update)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AdminTemplateDto>>>> AdminTemplates(CancellationToken ct)
    {
        var r = await sender.Send(new GetAdminTemplatesQuery(), ct);
        return Ok(ApiResponse<IReadOnlyList<AdminTemplateDto>>.Ok(r.Data, r.Message));
    }

    /// <summary>Admin: edit a template's metadata + published flag.</summary>
    [HttpPut("templates/{id:guid}")]
    [PermissionAuthorize(Permissions.Classes.Update)]
    public async Task<ActionResult<ApiResponse<AdminTemplateDto>>> UpdateTemplate(
        Guid id, [FromBody] UpdateTemplateCommand body, CancellationToken ct)
    {
        var r = await sender.Send(body with { Id = id }, ct);
        if (r.Success) return Ok(ApiResponse<AdminTemplateDto>.Ok(r.Data, r.Message));
        return r.ErrorCode == "NOT_FOUND"
            ? NotFound(ApiResponse<AdminTemplateDto>.Fail(r.Message ?? "Not found"))
            : BadRequest(ApiResponse<AdminTemplateDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>Admin: delete a template (blocked while any class uses it).</summary>
    [HttpDelete("templates/{id:guid}")]
    [PermissionAuthorize(Permissions.Classes.Update)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTemplate(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new DeleteTemplateCommand(id), ct);
        if (r.Success) return Ok(ApiResponse<bool>.Ok(r.Data, r.Message));
        return r.ErrorCode == "NOT_FOUND"
            ? NotFound(ApiResponse<bool>.Fail(r.Message ?? "Not found"))
            : BadRequest(ApiResponse<bool>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>The reusable day-by-day teaching plan for a template (Day 1 = 1A + 1B …).</summary>
    [HttpGet("templates/{id:guid}/plan")]
    public async Task<ActionResult<ApiResponse<TemplatePlanDto>>> Plan(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetTemplatePlanQuery(id), ct);
        return r.Success
            ? Ok(ApiResponse<TemplatePlanDto>.Ok(r.Data, r.Message))
            : NotFound(ApiResponse<TemplatePlanDto>.Fail(r.Message ?? "Not found"));
    }

    /// <summary>A class's journey through its day-plan (24 day-cards + completion).</summary>
    [HttpGet("class/{classId:guid}/plan-progress")]
    public async Task<ActionResult<ApiResponse<ClassPlanProgressDto>>> ClassPlanProgress(Guid classId, CancellationToken ct)
    {
        var r = await sender.Send(new GetClassPlanProgressQuery(classId), ct);
        if (r.Success) return Ok(ApiResponse<ClassPlanProgressDto>.Ok(r.Data, r.Message));
        return r.ErrorCode == "FORBIDDEN"
            ? StatusCode(403, ApiResponse<ClassPlanProgressDto>.Fail(r.Message ?? "Forbidden"))
            : NotFound(ApiResponse<ClassPlanProgressDto>.Fail(r.Message ?? "Not found"));
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

    /// <summary>
    /// F6: suggests the class's current curriculum position from its schedule
    /// pattern (StartDate + cadence). Always returns the ordered lesson list so the
    /// admin can edit the suggestion; with no pattern, CanSuggest is false.
    /// </summary>
    [HttpGet("class/{classId:guid}/suggest-position")]
    [PermissionAuthorize(Permissions.Classes.Update)]
    public async Task<ActionResult<ApiResponse<SuggestPositionDto>>> SuggestPosition(Guid classId, CancellationToken ct)
    {
        var r = await sender.Send(new SuggestPositionQuery(classId), ct);
        return r.Success
            ? Ok(ApiResponse<SuggestPositionDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<SuggestPositionDto>.Fail(r.Message ?? "Failed"));
    }

    /// <summary>
    /// F6 (LIVE DATA): marks every lesson before the chosen one Completed via
    /// backfilled sessions. Idempotent + reconciling — re-run with a corrected
    /// lesson to fix a mistake; excess backfill is removed, real sessions untouched.
    /// Body: {"lessonId":"..."}.
    /// </summary>
    [HttpPost("class/{classId:guid}/set-position")]
    [PermissionAuthorize(Permissions.Classes.Update)]
    public async Task<ActionResult<ApiResponse<SetPositionResultDto>>> SetPosition(
        Guid classId, [FromBody] SetPositionCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { ClassId = classId }, ct);
        return r.Success
            ? Ok(ApiResponse<SetPositionResultDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<SetPositionResultDto>.Fail(r.Message ?? "Failed"));
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

    /// <summary>The class's curriculum as a student learning journey (units → lessons, with progress + locks).</summary>
    [HttpGet("class/{classId:guid}/roadmap")]
    public async Task<ActionResult<ApiResponse<StudentRoadmapDto>>> Roadmap(Guid classId, CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentRoadmapQuery(classId), ct);
        return r.Success
            ? Ok(ApiResponse<StudentRoadmapDto>.Ok(r.Data, r.Message))
            : r.ErrorCode == "FORBIDDEN"
                ? StatusCode(StatusCodes.Status403Forbidden, ApiResponse<StudentRoadmapDto>.Fail(r.Message ?? "Forbidden"))
                : NotFound(ApiResponse<StudentRoadmapDto>.Fail(r.Message ?? "Not found"));
    }

    // ===== Course Builder (teacher of the class / admin) ===================
    // Every endpoint returns the whole refreshed course so the roadmap re-renders
    // from one response. NO permission attribute here on purpose: teachers don't
    // hold Classes.Update (that's admin-only, for editing class settings), yet
    // they must build their OWN class's course. Authorization is self-scoped in
    // the handler (WithCourse → class teacher or admin), matching the lesson hub
    // and schedule-curriculum endpoints. FORBIDDEN → 403 via MapBuilder.

    private ActionResult<ApiResponse<ClassCourseBuilderDto>> MapBuilder(Result<ClassCourseBuilderDto> r) =>
        r.Success
            ? Ok(ApiResponse<ClassCourseBuilderDto>.Ok(r.Data, r.Message))
            : r.ErrorCode switch
            {
                "NOT_FOUND" => NotFound(ApiResponse<ClassCourseBuilderDto>.Fail(r.Message ?? "Not found")),
                "FORBIDDEN" => StatusCode(StatusCodes.Status403Forbidden, ApiResponse<ClassCourseBuilderDto>.Fail(r.Message ?? "Forbidden")),
                _ => BadRequest(ApiResponse<ClassCourseBuilderDto>.Fail(r.Message ?? "Failed")),
            };

    /// <summary>Reads (and lazily provisions) the class's editable course structure.</summary>
    [HttpGet("class/{classId:guid}/builder")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> Builder(Guid classId, CancellationToken ct)
        => MapBuilder(await sender.Send(new GetClassCourseBuilderQuery(classId), ct));

    /// <summary>One-click: deep-copy a curriculum template into this class as its editable course.</summary>
    [HttpPost("class/{classId:guid}/use-template/{templateId:guid}")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> UseTemplate(
        Guid classId, Guid templateId, CancellationToken ct)
        => MapBuilder(await sender.Send(new CloneTemplateToClassCommand(classId, templateId), ct));

    /// <summary>
    /// F3 one-click course setup — clone the template, generate the 2–3 lessons/day
    /// session calendar, and map sessions to lessons, atomically. Body carries the
    /// schedule window + daily slots: {"templateId","type","daysOfWeekMask",
    /// "startDate","endDate","slots":[{"startsAt","endsAt"}],"roomId"}.
    /// </summary>
    [HttpPost("class/{classId:guid}/generate-course")]
    [PermissionAuthorize(Permissions.Classes.Update)]
    public async Task<ActionResult<ApiResponse<GenerateCourseResultDto>>> GenerateCourse(
        Guid classId, [FromBody] GenerateCourseCommand cmd, CancellationToken ct)
    {
        var r = await sender.Send(cmd with { ClassId = classId }, ct);
        return r.Success
            ? Ok(ApiResponse<GenerateCourseResultDto>.Ok(r.Data, r.Message))
            : r.ErrorCode == "NOT_FOUND"
                ? NotFound(ApiResponse<GenerateCourseResultDto>.Fail(r.Message ?? "Not found"))
                : BadRequest(ApiResponse<GenerateCourseResultDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("units")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> CreateUnit(
        [FromBody] CreateCourseUnitCommand cmd, CancellationToken ct)
        => MapBuilder(await sender.Send(cmd, ct));

    [HttpPut("units/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> UpdateUnit(
        Guid id, [FromBody] UpdateCourseUnitCommand cmd, CancellationToken ct)
        => MapBuilder(await sender.Send(cmd with { UnitId = id }, ct));

    [HttpDelete("units/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> DeleteUnit(Guid id, CancellationToken ct)
        => MapBuilder(await sender.Send(new DeleteCourseUnitCommand(id), ct));

    [HttpPost("units/reorder")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> ReorderUnits(
        [FromBody] ReorderCourseUnitsCommand cmd, CancellationToken ct)
        => MapBuilder(await sender.Send(cmd, ct));

    [HttpPost("lessons")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> CreateLesson(
        [FromBody] CreateCourseLessonCommand cmd, CancellationToken ct)
        => MapBuilder(await sender.Send(cmd, ct));

    [HttpPut("lessons/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> UpdateLesson(
        Guid id, [FromBody] UpdateCourseLessonCommand cmd, CancellationToken ct)
        => MapBuilder(await sender.Send(cmd with { LessonId = id }, ct));

    [HttpDelete("lessons/{id:guid}")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> DeleteLesson(Guid id, CancellationToken ct)
        => MapBuilder(await sender.Send(new DeleteCourseLessonCommand(id), ct));

    [HttpPost("lessons/reorder")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> ReorderLessons(
        [FromBody] ReorderCourseLessonsCommand cmd, CancellationToken ct)
        => MapBuilder(await sender.Send(cmd, ct));

    /// <summary>Create a whole unit and all its lessons in one request (the one-click builder flow).</summary>
    [HttpPost("units/bulk")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> BulkCreateUnit(
        [FromBody] BulkCreateUnitCommand cmd, CancellationToken ct)
        => MapBuilder(await sender.Send(cmd, ct));

    /// <summary>Duplicate a unit and all of its lessons (appended to the end of the course).</summary>
    [HttpPost("units/{id:guid}/duplicate")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> DuplicateUnit(Guid id, CancellationToken ct)
        => MapBuilder(await sender.Send(new DuplicateCourseUnitCommand(id), ct));

    /// <summary>Duplicate a lesson within its unit.</summary>
    [HttpPost("lessons/{id:guid}/duplicate")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> DuplicateLesson(Guid id, CancellationToken ct)
        => MapBuilder(await sender.Send(new DuplicateCourseLessonCommand(id), ct));

    /// <summary>Move a lesson to another unit in the same course. Body: {"targetUnitId":"...","targetOrder":3}.</summary>
    [HttpPost("lessons/{id:guid}/move")]
    public async Task<ActionResult<ApiResponse<ClassCourseBuilderDto>>> MoveLesson(
        Guid id, [FromBody] MoveCourseLessonCommand cmd, CancellationToken ct)
        => MapBuilder(await sender.Send(cmd with { LessonId = id }, ct));
}
