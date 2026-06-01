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
    /// Queue <paramref name="markdownText"/> for delivery. The bool reports
    /// whether the message was accepted into the queue (<c>false</c> means
    /// notifier disabled by config, text is blank, or the queue is full).
    /// <b>Never throws.</b>
    /// </summary>
    Task<bool> SendAsync(string markdownText, CancellationToken cancellationToken = default);
}
