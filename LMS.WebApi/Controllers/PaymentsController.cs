using LMS.Application.Common.Models;
using LMS.Application.Common.Security;
using LMS.Application.Features.Payments;
using LMS.Domain.Enums;
using LMS.WebApi.Common;
using LMS.WebApi.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class PaymentsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [PermissionAuthorize(Permissions.Payments.Read)]
    public async Task<ActionResult<ApiResponse<PagedResult<PaymentDto>>>> All(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] PaymentStatus? status = null,
        CancellationToken ct = default)
    {
        var r = await sender.Send(new GetPaymentsQuery(page, pageSize, status), ct);
        return Ok(ApiResponse<PagedResult<PaymentDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("student/{studentProfileId:guid}")]
    [PermissionAuthorize(Permissions.Payments.Read)]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PaymentDto>>>> Student(Guid studentProfileId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentPaymentsQuery(studentProfileId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<PaymentDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("revenue")]
    [PermissionAuthorize(Permissions.Payments.Read)]
    public async Task<ActionResult<ApiResponse<decimal>>> Revenue(CancellationToken ct)
    {
        var r = await sender.Send(new GetRevenueSummaryQuery(), ct);
        return Ok(ApiResponse<decimal>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    [PermissionAuthorize(Permissions.Payments.Create)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Create([FromBody] CreatePaymentCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<PaymentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<PaymentDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/paid")]
    [PermissionAuthorize(Permissions.Payments.Update)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Paid(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new MarkPaymentPaidCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<PaymentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<PaymentDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/failed")]
    [PermissionAuthorize(Permissions.Payments.Update)]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Failed(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new MarkPaymentFailedCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<PaymentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<PaymentDto>.Fail(r.Message ?? "Failed"));
    }
}
