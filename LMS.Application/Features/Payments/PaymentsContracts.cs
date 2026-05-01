using LMS.Application.Common.Models;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Payments;

public sealed record PaymentDto(
    Guid Id,
    Guid StudentProfileId,
    decimal Amount,
    PaymentMethod Method,
    PaymentStatus Status);

public sealed record PaymentsPingCommand : IRequest<Result<string>>;

public sealed class PaymentsPingCommandHandler : IRequestHandler<PaymentsPingCommand, Result<string>>
{
    public Task<Result<string>> Handle(PaymentsPingCommand request, CancellationToken cancellationToken)
    {
        return Task.FromResult(Result<string>.Ok("Payments module ready"));
    }
}

public sealed record CreatePaymentCommand(Guid StudentProfileId, decimal Amount, PaymentMethod Method)
    : IRequest<Result<PaymentDto>>;

public sealed record MarkPaymentPaidCommand(Guid PaymentId) : IRequest<Result<PaymentDto>>;

public sealed record MarkPaymentFailedCommand(Guid PaymentId) : IRequest<Result<PaymentDto>>;

public sealed record GetStudentPaymentsQuery(Guid StudentProfileId) : IRequest<Result<IReadOnlyCollection<PaymentDto>>>;

public sealed record GetPaymentsQuery : IRequest<Result<IReadOnlyCollection<PaymentDto>>>;

public sealed record GetRevenueSummaryQuery : IRequest<Result<decimal>>;