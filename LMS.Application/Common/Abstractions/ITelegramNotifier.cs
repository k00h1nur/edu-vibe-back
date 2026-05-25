namespace LMS.Application.Common.Abstractions;

/// <summary>
/// Sends a plain-text or Markdown message to the academy's Telegram channel.
/// The implementation is fire-and-forget — if Telegram is unreachable or
/// misconfigured, it logs and swallows the error so the user-facing operation
/// (e.g. saving a visitor message) still succeeds.
/// </summary>
public interface ITelegramNotifier
{
    /// <summary>
    /// Sends <paramref name="markdownText"/> to the configured chat. Returns
    /// <c>true</c> if Telegram accepted the message, <c>false</c> on any failure
    /// (network, mis-config, API rejection). Never throws.
    /// </summary>
    Task<bool> SendAsync(string markdownText, CancellationToken cancellationToken = default);
}
