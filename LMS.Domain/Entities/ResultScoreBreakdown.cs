using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class ResultScoreBreakdown : BaseEntity
{
    public ResultScoreBreakdown(Guid resultId, string key, string value) : base()
    {
        if (resultId == Guid.Empty) throw new DomainException("Result id is required.");
        if (string.IsNullOrWhiteSpace(key)) throw new DomainException("Breakdown key is required.");
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException("Breakdown value is required.");
        ResultId = resultId;
        Key = key.Trim();
        Value = value.Trim();
    }

    public Guid ResultId { get; private set; }
    public ResultEntry? Result { get; private set; }
    public string Key { get; private set; }
    public string Value { get; private set; }
    public bool IsDeleted { get; private set; }

    public void SoftDelete() => IsDeleted = true;
}
