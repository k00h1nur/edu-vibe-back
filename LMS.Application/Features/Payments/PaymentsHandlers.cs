using LMS.Application.Common.Abstractions;
using LMS.Application.Common.Models;
using LMS.Application.Common.Salary;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LMS.Application.Features.Payments;

public sealed class PaymentsHandlers(IApplicationDbContext db, ISalaryCalculator salary) :
    IRequestHandler<CreatePaymentCommand, Result<PaymentDto>>,
    IRequestHandler<MarkPaymentPaidCommand, Result<PaymentDto>>,
    IRequestHandler<MarkPaymentFailedCommand, Result<PaymentDto>>,
    IRequestHandler<GetStudentPaymentsQuery, Result<IReadOnlyCollection<PaymentDto>>>,
    IRequestHandler<GetPaymentsQuery, Result<PagedResult<PaymentDto>>>,
    IRequestHandler<GetRevenueSummaryQuery, Result<decimal>>,
    IRequestHandler<GetTeacherSalaryQuery, Result<SalaryBreakdown>>,
    IRequestHandler<SetTeacherSalaryConfigCommand, Result<TeacherSalaryConfigDto>>,
    IRequestHandler<GetTeacherSalaryConfigsQuery, Result<IReadOnlyCollection<TeacherSalaryConfigDto>>>,
    IRequestHandler<DeleteTeacherSalaryConfigCommand, Result>,
    IRequestHandler<SetClassMonthlyPriceCommand, Result>
{
    public async Task<Result<PaymentDto>> Handle(CreatePaymentCommand request, CancellationToken cancellationToken)
    {
        Payment p;
        try
        {
            p = new Payment(request.StudentProfileId, request.ClassId, request.PeriodMonth, request.Amount, request.Method);
        }
        catch (LMS.Domain.Exceptions.DomainException ex)
        {
            return Result<PaymentDto>.Fail("VALIDATION", ex.Message);
        }
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
        if (request.ClassId is { } cid) query = query.Where(p => p.ClassId == cid);
        if (request.Month is { } m)
        {
            var first = new DateOnly(m.Year, m.Month, 1);
            query = query.Where(p => p.PeriodMonth == first);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .Select(p => new PaymentDto(p.Id, p.StudentProfileId, p.ClassId, p.PeriodMonth, p.Amount, p.Method, p.Status))
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
            .Select(p => new PaymentDto(p.Id, p.StudentProfileId, p.ClassId, p.PeriodMonth, p.Amount, p.Method, p.Status))
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

    public async Task<Result<SalaryBreakdown>> Handle(GetTeacherSalaryQuery request, CancellationToken ct)
    {
        var month = new DateOnly(request.Month.Year, request.Month.Month, 1);

        var classIds = await db.Classes.AsNoTracking()
            .Where(c => c.TeacherUserId == request.TeacherId).Select(c => c.Id).ToListAsync(ct);

        var revenueByClass = await db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Paid && p.ClassId != null
                && classIds.Contains(p.ClassId!.Value) && p.PeriodMonth == month)
            .GroupBy(p => p.ClassId!.Value)
            .Select(g => new { ClassId = g.Key, Revenue = g.Sum(x => x.Amount) })
            .ToListAsync(ct);

        var configs = await db.TeacherSalaryConfigs.AsNoTracking()
            .Where(s => s.TeacherId == request.TeacherId).ToListAsync(ct);
        var defaultPct = configs.FirstOrDefault(s => s.ClassId == null)?.Percentage;
        var overrideByClass = configs.Where(s => s.ClassId != null)
            .ToDictionary(s => s.ClassId!.Value, s => s.Percentage);

        var classRevenues = revenueByClass.Select(r => new ClassRevenue(
            r.ClassId, r.Revenue, overrideByClass.TryGetValue(r.ClassId, out var ov) ? ov : (decimal?)null)).ToList();

        var punishments = (await db.Punishments.AsNoTracking()
                .Where(p => p.TeacherId == request.TeacherId && p.PeriodMonth == month).ToListAsync(ct))
            .Select(p => new PunishmentLine(p.Id, p.Type, p.Value, p.Title)).ToList();

        var breakdown = salary.Calculate(new SalaryInput(defaultPct, classRevenues, punishments));
        return Result<SalaryBreakdown>.Ok(breakdown);
    }

    public async Task<Result<TeacherSalaryConfigDto>> Handle(SetTeacherSalaryConfigCommand request, CancellationToken ct)
    {
        if (!await db.Users.AsNoTracking().AnyAsync(u => u.Id == request.TeacherId, ct))
            return Result<TeacherSalaryConfigDto>.Fail("NOT_FOUND", "Teacher not found.");

        var existing = await db.TeacherSalaryConfigs
            .FirstOrDefaultAsync(s => s.TeacherId == request.TeacherId && s.ClassId == request.ClassId, ct);
        try
        {
            if (existing is null)
            {
                existing = new TeacherSalaryConfig(request.TeacherId, request.ClassId, request.Percentage);
                await db.TeacherSalaryConfigs.AddAsync(existing, ct);
            }
            else
            {
                existing.SetPercentage(request.Percentage);
            }
            await db.SaveChangesAsync(ct);
            return Result<TeacherSalaryConfigDto>.Ok(
                new TeacherSalaryConfigDto(existing.Id, existing.TeacherId, existing.ClassId, existing.Percentage), "Saved.");
        }
        catch (DomainException ex)
        {
            return Result<TeacherSalaryConfigDto>.Fail("VALIDATION", ex.Message);
        }
    }

    public async Task<Result<IReadOnlyCollection<TeacherSalaryConfigDto>>> Handle(
        GetTeacherSalaryConfigsQuery request, CancellationToken ct)
    {
        var rows = await db.TeacherSalaryConfigs.AsNoTracking()
            .Where(s => s.TeacherId == request.TeacherId)
            .Select(s => new TeacherSalaryConfigDto(s.Id, s.TeacherId, s.ClassId, s.Percentage))
            .ToListAsync(ct);
        return Result<IReadOnlyCollection<TeacherSalaryConfigDto>>.Ok(rows);
    }

    public async Task<Result> Handle(DeleteTeacherSalaryConfigCommand request, CancellationToken ct)
    {
        var s = await db.TeacherSalaryConfigs.FirstOrDefaultAsync(x => x.Id == request.Id, ct);
        if (s is null) return Result.Fail("NOT_FOUND", "Config not found.");
        db.TeacherSalaryConfigs.Remove(s);
        await db.SaveChangesAsync(ct);
        return Result.Ok("Deleted.");
    }

    public async Task<Result> Handle(SetClassMonthlyPriceCommand request, CancellationToken ct)
    {
        var c = await db.Classes.FirstOrDefaultAsync(x => x.Id == request.ClassId, ct);
        if (c is null) return Result.Fail("NOT_FOUND", "Class not found.");
        try
        {
            c.SetMonthlyPrice(request.MonthlyPrice);
        }
        catch (DomainException ex)
        {
            return Result.Fail("VALIDATION", ex.Message);
        }
        await db.SaveChangesAsync(ct);
        return Result.Ok("Saved.");
    }

    private static PaymentDto Map(Payment p)
    {
        return new PaymentDto(p.Id, p.StudentProfileId, p.ClassId, p.PeriodMonth, p.Amount, p.Method, p.Status);
    }
}