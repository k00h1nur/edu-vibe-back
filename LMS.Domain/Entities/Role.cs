using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class Role : BaseEntity
{
    public Role(string code, string name) : base()
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Role code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Role name is required.");
        Code = code.Trim();
        Name = name.Trim();
    }

    public string Code { get; private set; }
    public string Name { get; private set; }
    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();

    public void Update(string code, string name)
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Role code is required.");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("Role name is required.");
        Code = code.Trim();
        Name = name.Trim();
        Touch();
    }
}
