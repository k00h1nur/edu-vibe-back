namespace LMS.Application.Common.Abstractions;

/// <summary>
/// Read-only access to the platform Telegram bot configuration that lives in
/// server config (appsettings / env), not the database. The bot username and
/// Mini App URL are fixed by the operator — admins can no longer change the bot
/// from the UI — so the Application layer reads them through this abstraction
/// instead of a DB row.
/// </summary>
public interface ITelegramConfig
{
    /// <summary>Bot @username (leading @ stripped), or null when unset. Public, safe to expose.</summary>
    string? BotUsername { get; }

    /// <summary>Production Mini App base URL (e.g. https://admin.edu-vibe.uz), trailing slash trimmed, or null.</summary>
    string? MiniAppUrl { get; }
}
