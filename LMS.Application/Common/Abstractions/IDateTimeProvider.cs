namespace LMS.Application.Common.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}