namespace LMS.Application.Common.Abstractions;

/// <summary>The verified Telegram user extracted from a valid WebApp initData.</summary>
public sealed class TelegramInitData
{
    public long UserId { get; init; }
    public string? Username { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? PhotoUrl { get; init; }
}

/// <summary>
/// Verifies the signature + freshness of Telegram Mini App <c>initData</c>
/// (the signed payload <c>window.Telegram.WebApp.initData</c>). On success the
/// caller can trust the returned user identity came from Telegram.
/// </summary>
public interface ITelegramInitDataValidator
{
    /// <summary>
    /// Returns the verified user on success, or <c>(null, error)</c> on failure
    /// (bad signature, stale auth_date, missing user, or unconfigured bot token).
    /// </summary>
    (TelegramInitData? User, string? Error) Validate(string initData);
}
