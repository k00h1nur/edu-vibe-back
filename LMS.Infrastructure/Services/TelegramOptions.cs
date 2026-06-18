namespace LMS.Infrastructure.Services;

/// <summary>
/// Strongly-typed binding for the "Telegram" section of appsettings / env.
/// Read once at startup and injected via IOptions to avoid the per-call
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> indexer.
/// </summary>
public sealed class TelegramOptions
{
    public const string SectionName = "Telegram";

    /// <summary>
    /// Platform bot token (@platform_eduvibeBot) from @BotFather. Used for Mini App
    /// initData verification AND for direct messages to students (the bot they
    /// signed in with — the only bot allowed to DM them).
    /// </summary>
    public string? BotToken { get; init; }

    /// <summary>
    /// Manager bot token (@eduvibe_manager_bot) — used for marketing + the staff/admin
    /// group notifications (the <see cref="ChatId"/> group). Falls back to
    /// <see cref="BotToken"/> when unset so existing group notifications keep working.
    /// </summary>
    public string? ManagerBotToken { get; init; }

    /// <summary>Target chat id (negative for groups/channels) for staff/marketing notices.</summary>
    public string? ChatId { get; init; }

    /// <summary>
    /// Public bot @username (with or without the leading @), e.g. "platform_eduvibeBot".
    /// Surfaced to the panels so "Open in Telegram" / deep links target the right bot.
    /// Set in server config only — admins can no longer change the bot from the UI.
    /// </summary>
    public string? BotUsername { get; init; }

    /// <summary>
    /// Production Mini App base URL (e.g. https://admin.edu-vibe.uz). Used to register
    /// the bot's default menu button (the Mini App launcher) on startup and as the
    /// canonical origin for the deep-link handoff. Must be https for Telegram to accept it.
    /// </summary>
    public string? MiniAppUrl { get; init; }

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

    /// <summary>Token used for the staff/marketing group: the manager bot, or the platform bot as fallback.</summary>
    public string? GroupBotToken =>
        !string.IsNullOrWhiteSpace(ManagerBotToken) ? ManagerBotToken : BotToken;

    /// <summary>Group/marketing notifications can be sent (a group token + a chat id are set).</summary>
    public bool GroupEnabled =>
        !string.IsNullOrWhiteSpace(GroupBotToken) && !string.IsNullOrWhiteSpace(ChatId);

    /// <summary>Direct messages to users can be sent (the platform bot token is set).</summary>
    public bool DmEnabled => !string.IsNullOrWhiteSpace(BotToken);

    /// <summary>True when the sender worker has anything it could deliver.</summary>
    public bool IsEnabled => GroupEnabled || DmEnabled;
}
