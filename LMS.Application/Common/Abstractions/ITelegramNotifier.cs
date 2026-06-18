namespace LMS.Application.Common.Abstractions;

/// <summary>
/// Enqueues a Telegram notification for the academy's chat. Returns immediately
/// after writing to the in-process queue — the actual HTTP call is performed by
/// a background worker with retry + backoff and respects Telegram's 429
/// retry_after. Callers can treat this as fire-and-forget; no exceptions
/// surface to user-facing code paths (e.g. saving a visitor message).
/// </summary>
public interface ITelegramNotifier
{
    /// <summary>
    /// Queue <paramref name="markdownText"/> for delivery to the academy's staff /
    /// marketing group (via the manager bot). The bool reports whether the message
    /// was accepted into the queue (<c>false</c> means group notifier disabled by
    /// config, text is blank, or the queue is full). <b>Never throws.</b>
    /// </summary>
    Task<bool> SendAsync(string markdownText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue a plain-text direct message to a single Telegram user (by their
    /// Telegram user id) via the platform bot — the only bot that may DM students,
    /// since it's the one they signed in with. No-op (returns <c>false</c>) when
    /// the platform bot token isn't configured. <b>Never throws.</b>
    /// </summary>
    Task<bool> SendToUserAsync(long telegramUserId, string text, CancellationToken cancellationToken = default);
}
