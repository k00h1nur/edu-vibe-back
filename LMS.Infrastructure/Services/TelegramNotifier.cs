using System.Net.Http.Json;
using LMS.Application.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Sends MarkdownV2 messages to a Telegram chat via the Bot API.
///
/// Config keys (appsettings.json or env):
///   Telegram:BotToken — bot token from @BotFather. Required.
///   Telegram:ChatId   — target chat id (negative for groups/channels). Required.
///
/// If either is missing the notifier is a no-op and logs once on first call.
/// Never throws — callers can treat <see cref="SendAsync"/> as best-effort.
/// </summary>
public sealed class TelegramNotifier(
    IHttpClientFactory httpFactory,
    IConfiguration configuration,
    ILogger<TelegramNotifier> logger)
    : ITelegramNotifier
{
    private const string HttpClientName = "Telegram";

    public async Task<bool> SendAsync(string markdownText, CancellationToken cancellationToken = default)
    {
        var botToken = configuration["Telegram:BotToken"];
        var chatId = configuration["Telegram:ChatId"];

        if (string.IsNullOrWhiteSpace(botToken) || string.IsNullOrWhiteSpace(chatId))
        {
            logger.LogWarning(
                "Telegram notifier is disabled — set Telegram:BotToken and Telegram:ChatId in configuration.");
            return false;
        }

        try
        {
            var http = httpFactory.CreateClient(HttpClientName);
            var url = $"https://api.telegram.org/bot{botToken}/sendMessage";
            var payload = new
            {
                chat_id = chatId,
                text = markdownText,
                parse_mode = "MarkdownV2",
                disable_web_page_preview = true,
            };

            using var response = await http.PostAsJsonAsync(url, payload, cancellationToken);
            if (response.IsSuccessStatusCode) return true;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "Telegram sendMessage returned {Status}: {Body}", (int)response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Telegram sendMessage threw");
            return false;
        }
    }
}
