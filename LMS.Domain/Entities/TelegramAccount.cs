using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// Links a platform <see cref="User"/> to their Telegram identity (1:1). Created
/// when a user authenticates through the Telegram Mini App or links from the web.
/// The Telegram fields are a verified snapshot from the signed WebApp initData.
/// </summary>
public sealed class TelegramAccount : BaseEntity
{
    private TelegramAccount() { }

    public TelegramAccount(
        Guid userId,
        long telegramUserId,
        string? username,
        string? firstName,
        string? lastName,
        string? photoUrl)
    {
        if (userId == Guid.Empty) throw new DomainException("User id is required.");
        if (telegramUserId <= 0) throw new DomainException("Telegram user id is required.");

        UserId = userId;
        TelegramUserId = telegramUserId;
        Username = Trim(username, 64);
        FirstName = Trim(firstName, 128);
        LastName = Trim(lastName, 128);
        PhotoUrl = Trim(photoUrl, 1024);
        LinkedAt = DateTime.UtcNow;
    }

    public Guid UserId { get; private set; }
    public User? User { get; private set; }

    /// <summary>Telegram's numeric user id (stable identity).</summary>
    public long TelegramUserId { get; private set; }
    public string? Username { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? PhotoUrl { get; private set; }
    public DateTime LinkedAt { get; private set; }

    /// <summary>Refresh the cached Telegram profile snapshot on each sign-in.</summary>
    public void UpdateProfile(string? username, string? firstName, string? lastName, string? photoUrl)
    {
        Username = Trim(username, 64);
        FirstName = Trim(firstName, 128);
        LastName = Trim(lastName, 128);
        PhotoUrl = Trim(photoUrl, 1024);
        Touch();
    }

    private static string? Trim(string? value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var t = value.Trim();
        return t.Length > max ? t[..max] : t;
    }
}
