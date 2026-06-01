using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Payments;

public sealed class PaymentsHandlers(IApplicationDbContext db) :
    IRequestHandler<CreatePaymentCommand, Result<PaymentDto>>,
    IRequestHandler<MarkPaymentPaidCommand, Result<PaymentDto>>,
    IRequestHandler<MarkPaymentFailedCommand, Result<PaymentDto>>,
    IRequestHandler<GetStudentPaymentsQuery, Result<IReadOnlyCollection<PaymentDto>>>,
    IRequestHandler<GetPaymentsQuery, Result<PagedResult<PaymentDto>>>,
    IRequestHandler<GetRevenueSummaryQuery, Result<decimal>>
{
    public async Task<Result<PaymentDto>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        var p = new Payment(request.StudentProfileId, request.Amount, request.Method);
        await db.Payments.AddAsync(p, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return Result<PaymentDto>.Ok(Map(p));
    }

    public async Task<Result<PagedResult<PaymentDto>>> Handle(GetPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        var page = new PageRequest(request.Page, request.PageSize);

        var query = db.Payments.AsQueryable();
        if (request.Status is { } status) query = query.Where(p => p.Status == status);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(p => new PaymentDto(p.Id, p.StudentProfileId, p.Amount, p.Method, p.Status))
            .ToListAsync(cancellationToken);

        return Result<PagedResult<PaymentDto>>.Ok(PagedResult<PaymentDto>.From(items, total, page));
    }

    public async Task<Result<decimal>> Handle(GetRevenueSummaryQuery request, CancellationToken cancellationToken)
    {
        return Result<decimal>.Ok(await db.Payments.Where(x => x.Status == PaymentStatus.Paid)
            .SumAsync(x => x.Amount, cancellationToken));
    }

    public async Task<Result<IReadOnlyCollection<PaymentDto>>> Handle(GetStudentPaymentsQuery request,
        CancellationToken cancellationToken)
    {
        return Result<IReadOnlyCollection<PaymentDto>>.Ok(await db.Payments
            .Where(x => x.StudentProfileId == request.StudentProfileId)
            .Select(p => new PaymentDto(p.Id, p.StudentProfileId, p.Amount, p.Method, p.Status))
            .ToListAsync(cancellationToken));
    }

    public async Task<Result<PaymentDto>> Handle(MarkPaymentFailedCommand request, CancellationToken cancellationToken)
    {
        var p = await db.Payments.FirstOrDefaultAsync(x => x.Id == request.PaymentId, cancellationToken);
        if (p is null) return Result<PaymentDto>.Fail("NOT_FOUND", "Payment not found.");
        p.MarkFailed();
        await db.SaveChangesAsync(cancellationToken);
        return Result<PaymentDto>.Ok(Map(p));
    }

    public async Task<Result<PaymentDto>> Handle(MarkPaymentPaidCommand request, CancellationToken cancellationToken)
    {
        var p = await db.Payments.FirstOrDefaultAsync(x => x.Id == request.PaymentId, cancellationToken);
        if (p is null) return Result<PaymentDto>.Fail("NOT_FOUND", "Payment not found.");
        p.MarkPaid();
        await db.SaveChangesAsync(cancellationToken);
        return Result<PaymentDto>.Ok(Map(p));
    }

    private static PaymentDto Map(Payment p)
    {
        return new PaymentDto(p.Id, p.StudentProfileId, p.Amount, p.Method, p.Status);
    }
}