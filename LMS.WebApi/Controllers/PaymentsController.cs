using LMS.Application.Features.Payments;
using LMS.WebApi.Common;
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
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PaymentDto>>>> All(CancellationToken ct)
    {
        var r = await sender.Send(new GetPaymentsQuery(), ct);
        return Ok(ApiResponse<IReadOnlyCollection<PaymentDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("student/{studentProfileId:guid}")]
    public async Task<ActionResult<ApiResponse<IReadOnlyCollection<PaymentDto>>>> Student(Guid studentProfileId,
        CancellationToken ct)
    {
        var r = await sender.Send(new GetStudentPaymentsQuery(studentProfileId), ct);
        return Ok(ApiResponse<IReadOnlyCollection<PaymentDto>>.Ok(r.Data, r.Message));
    }

    [HttpGet("revenue")]
    public async Task<ActionResult<ApiResponse<decimal>>> Revenue(CancellationToken ct)
    {
        var r = await sender.Send(new GetRevenueSummaryQuery(), ct);
        return Ok(ApiResponse<decimal>.Ok(r.Data, r.Message));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Create([FromBody] CreatePaymentCommand cmd,
        CancellationToken ct)
    {
        var r = await sender.Send(cmd, ct);
        return r.Success
            ? Ok(ApiResponse<PaymentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<PaymentDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/paid")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Paid(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new MarkPaymentPaidCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<PaymentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<PaymentDto>.Fail(r.Message ?? "Failed"));
    }

    [HttpPost("{id:guid}/failed")]
    public async Task<ActionResult<ApiResponse<PaymentDto>>> Failed(Guid id, CancellationToken ct)
    {
        var r = await sender.Send(new MarkPaymentFailedCommand(id), ct);
        return r.Success
            ? Ok(ApiResponse<PaymentDto>.Ok(r.Data, r.Message))
            : BadRequest(ApiResponse<PaymentDto>.Fail(r.Message ?? "Failed"));
    }
}