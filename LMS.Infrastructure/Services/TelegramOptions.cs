namespace LMS.Infrastructure.Services;

/// <summary>
/// Strongly-typed binding for the "Telegram" section of appsettings / env.
/// Read once at startup and injected via IOptions to avoid the per-call
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> indexer.
/// </summary>
public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>Bot token from @BotFather. Empty disables the notifier.</summary>
    public string? BotToken { get; init; }

    /// <summary>Target chat id (negative for groups/channels). Empty disables the notifier.</summary>
    public string? ChatId { get; init; }

    /// <summary>
    /// Bounded in-memory queue depth. New writes drop the oldest message when
    /// full so a Telegram outage can never block the API thread or balloon
    /// memory. Default 256 ≈ a few minutes of normal traffic.
    /// </summary>
    public int QueueCapacity { get; init; } = 256;

    /// <summary>Total attempts (including the first) before a message is dropped.</summary>
    public int MaxAttempts { get; init; } = 4;

    /// <summary>HTTP timeout per attempt, seconds.</summary>
    public int RequestTimeoutSeconds { get; init; } = 10;

    /// <summary>Base delay for exponential backoff between retries, milliseconds.</summary>
    public int BaseBackoffMilliseconds { get; init; } = 250;

    public bool IsEnabled =>
        !string.IsNullOrWhiteSpace(BotToken) && !string.IsNullOrWhiteSpace(ChatId);
}
