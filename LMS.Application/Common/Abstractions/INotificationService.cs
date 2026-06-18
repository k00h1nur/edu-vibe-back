namespace LMS.Application.Common.Abstractions;

/// <summary>
/// Sends a Telegram direct message to a platform user (any role — student,
/// teacher, or staff) by resolving their linked Telegram account and DMing them
/// through the platform bot (@platform_eduvibeBot), the bot every role signs in
/// with. Fire-and-forget: a user with no linked Telegram is silently skipped and
/// nothing throws, so callers can notify without guarding their own flow.
/// </summary>
public interface INotificationService
{
    /// <summary>DM a single user by their platform user id (no-op if not linked).</summary>
    Task NotifyUserAsync(Guid userId, string text, CancellationToken cancellationToken = default);

    /// <summary>DM several users by their platform user ids (skips unlinked ones).</summary>
    Task NotifyUsersAsync(IReadOnlyCollection<Guid> userIds, string text, CancellationToken cancellationToken = default);
}
