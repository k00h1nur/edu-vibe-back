using LMS.Domain.Common;
using LMS.Domain.Enums;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

public sealed class User : BaseEntity
{
    public User(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");

        Email = email.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        Status = UserStatus.Active;
    }

    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string? RefreshTokenHash { get; private set; }
    public DateTime? RefreshTokenExpiresAt { get; private set; }
    public UserStatus Status { get; private set; }

    public StudentProfile? StudentProfile { get; private set; }
    public StaffProfile? StaffProfile { get; private set; }

    public ICollection<UserRole> UserRoles { get; } = new List<UserRole>();
    public ICollection<Class> TeachingClasses { get; } = new List<Class>();

    public bool HasRole(string roleCode)
    {
        return UserRoles.Any(x => x.Role != null && x.Role.Code == roleCode);
    }

    public void SetPasswordHash(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");
        PasswordHash = passwordHash;
        Touch();
    }

    public void SetEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new DomainException("Email is required.");
        Email = email.Trim().ToLowerInvariant();
        Touch();
    }

    public void SetRefreshToken(string refreshTokenHash, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenHash)) throw new DomainException("Refresh token hash is required.");
        RefreshTokenHash = refreshTokenHash;
        RefreshTokenExpiresAt = expiresAt;
        Touch();
    }

    public void ClearRefreshToken()
    {
        RefreshTokenHash = null;
        RefreshTokenExpiresAt = null;
        Touch();
    }

    public void Deactivate()
    {
        Status = UserStatus.Inactive;
        Touch();
    }

    public void Activate()
    {
        Status = UserStatus.Active;
        Touch();
    }
}