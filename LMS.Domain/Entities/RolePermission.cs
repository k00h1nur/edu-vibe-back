using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class RolePermission : BaseEntity
{
    public RolePermission(Guid roleId, Guid permissionId) : base()
    {
        if (roleId == Guid.Empty) throw new DomainException("Role id is required.");
        if (permissionId == Guid.Empty) throw new DomainException("Permission id is required.");
        RoleId = roleId;
        PermissionId = permissionId;
    }

    public Guid RoleId { get; private set; }
    public Role? Role { get; private set; }
    public Guid PermissionId { get; private set; }
    public Permission? Permission { get; private set; }
}
