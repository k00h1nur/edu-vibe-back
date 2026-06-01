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
public sealed class TelegramNotifier : ITelegramNotifier
{
    private readonly Channel<string> _channel;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramNotifier> _logger;

    public TelegramNotifier(IOptions<TelegramOptions> options, ILogger<TelegramNotifier> logger)
    {
        _options = options.Value;
        _logger = logger;

        var capacity = _options.QueueCapacity > 0 ? _options.QueueCapacity : 256;
        _channel = Channel.CreateBounded<string>(new BoundedChannelOptions(capacity)
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
                "Telegram notifier disabled — set Telegram:BotToken and Telegram:ChatId to enable.");
        }
    }

    /// <summary>Consumed by <see cref="TelegramSenderHostedService"/>; not part of the public API.</summary>
    internal ChannelReader<string> Reader => _channel.Reader;

    public Task<bool> SendAsync(string markdownText, CancellationToken cancellationToken = default)
    {
        if (!_options.IsEnabled) return Task.FromResult(false);
        if (string.IsNullOrWhiteSpace(markdownText)) return Task.FromResult(false);

        if (_channel.Writer.TryWrite(markdownText)) return Task.FromResult(true);

        // BoundedChannelFullMode.DropOldest guarantees TryWrite always succeeds while
        // the channel is open, so reaching here means the channel was completed
        // (i.e. the host is shutting down). Log quietly and drop.
        _logger.LogWarning("Telegram queue closed — message dropped.");
        return Task.FromResult(false);
    }
}
