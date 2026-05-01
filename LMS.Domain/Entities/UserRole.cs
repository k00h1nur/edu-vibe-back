using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class UserRole : BaseEntity
{
    public UserRole(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty || roleId == Guid.Empty) throw new DomainException("User and role are required.");
        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }
    public User? User { get; private set; }
    public Guid RoleId { get; private set; }
    public Role? Role { get; private set; }
}