using LMS.Application.Common.Abstractions;
using Microsoft.Extensions.Options;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Surfaces the public, read-only bits of <see cref="TelegramOptions"/> (bot
/// username + Mini App URL) to the Application layer, normalising them so callers
/// never have to trim a leading @ or a trailing slash.
/// </summary>
internal sealed class TelegramConfig(IOptions<TelegramOptions> options) : ITelegramConfig
{
    private readonly TelegramOptions _options = options.Value;

    public string? BotUsername =>
        string.IsNullOrWhiteSpace(_options.BotUsername)
            ? null
            : _options.BotUsername.TrimStart('@').Trim();

    public string? MiniAppUrl =>
        string.IsNullOrWhiteSpace(_options.MiniAppUrl)
            ? null
            : _options.MiniAppUrl.Trim().TrimEnd('/');
}
