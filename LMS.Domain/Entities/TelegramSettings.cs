using LMS.Domain.Common;

namespace LMS.Domain.Entities;

/// <summary>
/// Singleton row holding platform-level Telegram bot configuration that is safe
/// to expose publicly — currently just the bot's @username, used to build the
/// "Open in Telegram" deep link (t.me/&lt;username&gt;) shown in the panels.
///
/// Secrets (bot token, chat id) deliberately stay in server config/env, never
/// here. Singleton like <see cref="OfficeInfo"/>: a fixed id makes admin upserts
/// deterministic (create on first save, update thereafter).
/// </summary>
public sealed class TelegramSettings : BaseEntity
{
    public static readonly Guid SingletonId = new("50000000-0000-0000-0000-000000000002");

    private TelegramSettings() { }

    public TelegramSettings(string? botUsername)
    {
        Id = SingletonId;
        SetBotUsername(botUsername);
    }

    /// <summary>The bot's public @username (without the leading @). Null = not configured.</summary>
    public string? BotUsername { get; private set; }

    /// <summary>Normalizes (strips a leading @, trims, caps length). Null/blank clears it.</summary>
    public void SetBotUsername(string? botUsername)
    {
        if (string.IsNullOrWhiteSpace(botUsername))
        {
            BotUsername = null;
        }
        else
        {
            var t = botUsername.Trim().TrimStart('@').Trim();
            BotUsername = t.Length == 0 ? null : (t.Length > 64 ? t[..64] : t);
        }
        Touch();
    }
}
