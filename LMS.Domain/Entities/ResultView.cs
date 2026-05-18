using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class ResultView : BaseEntity
{
    public ResultView(Guid resultId, string? ipAddress, string? userAgent) : base()
    {
        if (resultId == Guid.Empty) throw new DomainException("Result id is required.");
        ResultId = resultId;
        IpAddress = ipAddress?.Trim();
        UserAgent = userAgent?.Trim();
    }

    public Guid ResultId { get; private set; }
    public ResultEntry? Result { get; private set; }
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
}
