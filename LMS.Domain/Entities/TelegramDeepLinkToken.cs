using LMS.Domain.Common;
using LMS.Domain.Exceptions;

namespace LMS.Domain.Entities;

/// <summary>
/// A short-lived, one-time token that hands a signed-in web session over to the
/// Telegram Mini App. The panel mints one when the user clicks "Open in
/// Telegram", embeds it as the bot's <c>startapp</c> deep-link parameter, and
/// the Mini App exchanges it (together with verified initData) to sign in as the
/// SAME user — no password — linking their Telegram account in the process.
///
/// Security: random, bound to one user, expires fast (minutes) and is consumed
/// on first use, so a leaked link has a tiny window and works at most once.
/// </summary>
public sealed class TelegramDeepLinkToken : BaseEntity
{
    private TelegramDeepLinkToken() { }

    public TelegramDeepLinkToken(Guid userId, string token, DateTime expiresAtUtc)
    {
        if (userId == Guid.Empty) throw new DomainException("User id is required.");
        if (string.IsNullOrWhiteSpace(token)) throw new DomainException("Token is required.");

        UserId = userId;
        Token = token;
        ExpiresAt = expiresAtUtc;
    }

    public Guid UserId { get; private set; }
    public User? User { get; private set; }

    /// <summary>URL-safe random token, also used as the Telegram <c>startapp</c> param.</summary>
    public string Token { get; private set; } = null!;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? ConsumedAt { get; private set; }

    /// <summary>Usable = not yet consumed and not yet expired.</summary>
    public bool IsUsable(DateTime nowUtc) => ConsumedAt is null && nowUtc < ExpiresAt;

    /// <summary>Marks the token spent (one-time use).</summary>
    public void Consume(DateTime nowUtc)
    {
        ConsumedAt = nowUtc;
        Touch();
    }
}
