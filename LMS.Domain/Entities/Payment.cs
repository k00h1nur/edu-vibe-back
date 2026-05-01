using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class Payment : BaseEntity
{
    public Payment(Guid studentProfileId, decimal amount, PaymentMethod method)
    {
        if (studentProfileId == Guid.Empty) throw new DomainException("Student profile id is required.");

        if (amount <= 0) throw new DomainException("Amount must be greater than zero.");

        StudentProfileId = studentProfileId;
        Amount = amount;
        Method = method;
        Status = PaymentStatus.Pending;
    }

    public Guid StudentProfileId { get; private set; }
    public StudentProfile? StudentProfile { get; private set; }

    public decimal Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }

    public void MarkPaid()
    {
        if (Status == PaymentStatus.Paid) throw new DomainException("Payment is already paid.");

        Status = PaymentStatus.Paid;
        Touch();
    }
}