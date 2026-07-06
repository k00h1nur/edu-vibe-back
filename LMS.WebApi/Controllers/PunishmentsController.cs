using System.Globalization;
using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Punishments;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

/// <summary>Admin-only teacher-punishment CRUD. Feeds the teacher salary calculation (F5).</summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PunishmentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [PermissionAuthorize(Permissions.Punishments.Manage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PunishmentDto>>>> All(
        [FromQuery] Guid? teacherId, [FromQuery] string? month, CancellationToken ct)
    {
        var r = await sender.Send(new GetPunishmentsQuery(teacherId, ParseMonth(month)), ct);
        return Ok(ApiResponse<IReadOnlyCollection<PunishmentDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("teacher/{teacherId:guid}")]
    [PermissionAuthorize(Permissions.Punishments.Manage)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PunishmentDto>>>> ForTeacher(
        Guid teacherId, [FromQuery] string? month, CancellationToken ct)
    {
        var r = await sender.Send(new GetPunishmentsQuery(teacherId, ParseMonth(month)), ct);
        return Ok(ApiResponse<IReadOnlyCollection<PunishmentDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("{id:guid}")]
    [PermissionAuthorize(Permissions.Punishments.Manage)]
    public async Task<ActionResult<ApiResponse<PunishmentDto>>> Get(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new GetPunishmentByIdQuery(id), ct);
        return r.ToApiResultOrNotFound();
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Punishments.Manage)]
    public async Task<ActionResult<ApiResponse<PunishmentDto>>> Create(
        [FromBody] CreatePunishmentCommand cmd, CancellationToken ct)
        => MapResult(await sender.Send(cmd, ct));

    [HttpPut("{id:guid}")]
    [PermissionAuthorize(Permissions.Punishments.Manage)]
    public async Task<ActionResult<ApiResponse<PunishmentDto>>> Update(
        Guid id, [FromBody] UpdatePunishmentCommand cmd, CancellationToken ct)
        => MapResult(await sender.Send(cmd with { Id = id }, ct));

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(Permissions.Punishments.Manage)]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new DeletePunishmentCommand(id), ct);
        if (r.Success) return Ok(ApiResponse<object>.Ok(new { }, r.Message));
        return r.ErrorCode == "NOT_FOUND"
            ? NotFound(ApiResponse<object>.Fail(r.Message ?? "Not found"))
            : BadRequest(ApiResponse<object>.Fail(r.Message ?? "Failed"));
    }

    private ActionResult<ApiResponse<PunishmentDto>> MapResult(Result<PunishmentDto> r) => r.Success
        ? Ok(ApiResponse<PunishmentDto>.Ok(r.Data, r.Message))
        : r.ErrorCode switch
        {
            "NOT_FOUND" => NotFound(ApiResponse<PunishmentDto>.Fail(r.Message ?? "Not found")),
            _ => BadRequest(ApiResponse<PunishmentDto>.Fail(r.Message ?? "Failed")),
        };

    /// <summary>"YYYY-MM" → 1st-of-month DateOnly (the platform PeriodMonth convention); null on blank/invalid.</summary>
    private static DateOnly? ParseMonth(string? month)
        => string.IsNullOrWhiteSpace(month)
            ? null
            : DateOnly.TryParseExact(month + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d)
                ? d
                : null;
}
