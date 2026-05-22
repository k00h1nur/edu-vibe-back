namespace LMS.Application.Common.Abstractions;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);

    /// <summary>Student profile id from the <c>studentProfileId</c> JWT claim, if present.</summary>
    Guid? StudentProfileId { get; }

    /// <summary>Staff profile id from the <c>staffProfileId</c> JWT claim, if present.</summary>
    Guid? StaffProfileId { get; }
}
