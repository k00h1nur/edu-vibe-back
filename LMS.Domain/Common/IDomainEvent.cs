namespace LMS.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }
}