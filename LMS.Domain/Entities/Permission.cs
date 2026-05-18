using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class Permission : BaseEntity
{
    public Permission(string code, string module, string? description = null) : base()
    {
        if (string.IsNullOrWhiteSpace(code)) throw new DomainException("Permission code is required.");
        if (string.IsNullOrWhiteSpace(module)) throw new DomainException("Permission module is required.");
        Code = code.Trim();
        Module = module.Trim();
        Description = description?.Trim();
    }

    public string Code { get; private set; }
    public string Module { get; private set; }
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; } = true;

    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();
}