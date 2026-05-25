using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class Course : BaseEntity
{
    public Course(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Course code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Course name is required.");
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
    }

    public string Code { get; private set; }
    public string Name { get; private set; }

    public void Update(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Course code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Course name is required.");
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Touch();
    }
}