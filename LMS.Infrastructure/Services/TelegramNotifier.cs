using System.Threading.Channels;
using LMS.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Producer side of the Telegram pipeline. <see cref="SendAsync"/> writes the
/// markdown payload to an in-process bounded channel and returns immediately —
/// the HTTP call to api.telegram.org happens on a single background worker
/// (<see cref="TelegramSenderHostedService"/>) with retry + backoff.
///
/// Why a channel:
///   • Caller (web request) is never blocked on the network.
///   • One bounded queue ⇒ predictable memory under bursts and outages.
///   • Single reader ⇒ no Telegram rate-limit pile-on from concurrent sends.
///   • Graceful drain on shutdown via BackgroundService.StopAsync.
/// </summary>
/// <summary>One queued Telegram send: which bot, which chat, the text, and how to parse it.</summary>
internal sealed record TelegramOutbound(string BotToken, string ChatId, string Text, string? ParseMode);

public sealed class TelegramNotifier : ITelegramNotifier
{
    private readonly Channel<TelegramOutbound> _channel;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramNotifier> _logger;

    public TelegramNotifier(IOptions<TelegramOptions> options, ILogger<TelegramNotifier> logger)
    {
        _options = options.Value;
        _logger = logger;

        var capacity = _options.QueueCapacity > 0 ? _options.QueueCapacity : 256;
        _channel = Channel.CreateBounded<TelegramOutbound>(new BoundedChannelOptions(capacity)
        {
            // Drop the oldest queued payload when full — staleness beats memory pressure
            // for non-critical chat notifications.
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        if (!_options.IsEnabled)
        {
            _logger.LogInformation(
                "Telegram notifier disabled — set Telegram:BotToken (DMs) and/or " +
                "Telegram:ManagerBotToken + Telegram:ChatId (group) to enable.");
        }
    }

    /// <summary>Consumed by <see cref="TelegramSenderHostedService"/>; not part of the public API.</summary>
    internal ChannelReader<TelegramOutbound> Reader => _channel.Reader;

    public Task<bool> SendAsync(string markdownText, CancellationToken cancellationToken = default)
    {
        // Group / marketing notice via the manager bot (MarkdownV2, as before).
        if (!_options.GroupEnabled) return Task.FromResult(false);
        if (string.IsNullOrWhiteSpace(markdownText)) return Task.FromResult(false);
        return Task.FromResult(Enqueue(
            new TelegramOutbound(_options.GroupBotToken!, _options.ChatId!, markdownText, "MarkdownV2")));
    }

    public Task<bool> SendToUserAsync(long telegramUserId, string text, CancellationToken cancellationToken = default)
    {
        // Direct message to a student via the platform bot. Plain text (no
        // parse_mode) so we don't have to MarkdownV2-escape user/lesson content.
        if (!_options.DmEnabled) return Task.FromResult(false);
        if (string.IsNullOrWhiteSpace(text) || telegramUserId <= 0) return Task.FromResult(false);
        return Task.FromResult(Enqueue(
            new TelegramOutbound(_options.BotToken!, telegramUserId.ToString(), text, ParseMode: null)));
    }

    private bool Enqueue(TelegramOutbound message)
    {
        if (_channel.Writer.TryWrite(message)) return true;

        // BoundedChannelFullMode.DropOldest guarantees TryWrite always succeeds while
        // the channel is open, so reaching here means the channel was completed
        // (i.e. the host is shutting down). Log quietly and drop.
        _logger.LogWarning("Telegram queue closed — message dropped.");
        return false;
    }
}
