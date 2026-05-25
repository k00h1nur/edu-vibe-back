using LMS.Application.Common.Security;
using LMS.Application.Features.Dashboard;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class DashboardController(ISender sender) : ControllerBase
{
    [HttpGet("director")]
    [PermissionAuthorize(Permissions.Dashboard.Director)]
    public async Task<ActionResult<ApiResponse<DirectorDashboardDto>>> Director(CancellationToken ct)
    {
        var r = await sender.Send(new GetDirectorDashboardQuery(), ct);
        return Ok(ApiResponse<DirectorDashboardDto>.Ok(r.Data, r.Message));
    }

    [HttpGet("office-admin")]
    [PermissionAuthorize(Permissions.Dashboard.Office)]
    public async Task<ActionResult<ApiResponse<OfficeAdminDashboardDto>>> Office(CancellationToken ct)
    {
        var r = await sender.Send(new GetOfficeAdminDashboardQuery(), ct);
        return Ok(ApiResponse<OfficeAdminDashboardDto>.Ok(r.Data, r.Message));
    }

    [HttpGet("teacher/{teacherUserId:guid}")]
    [PermissionAuthorize(Permissions.Dashboard.Teacher)]
    public async Task<ActionResult<ApiResponse<TeacherDashboardDto>>> Teacher(Guid teacherUserId, CancellationToken ct)
    {
        var r = await sender.Send(new GetTeacherDashboardQuery(teacherUserId), ct);
        return Ok(ApiResponse<TeacherDashboardDto>.Ok(r.Data, r.Message));
    }

    [HttpGet("student/{studentProfileId:guid}")]
    [PermissionAuthorize(Permissions.Dashboard.Student)]
    public async Task<ActionResult<ApiResponse<StudentDashboardDto>>> Student(Guid studentProfileId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentDashboardQuery(studentProfileId), ct);
        return Ok(ApiResponse<StudentDashboardDto>.Ok(r.Data, r.Message));
    }
}
