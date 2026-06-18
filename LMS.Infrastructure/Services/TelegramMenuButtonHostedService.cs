using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LMS.Infrastructure.Services;

/// <summary>
/// On startup, registers the bot's <em>default menu button</em> as a Web App
/// launcher pointing at the production Mini App (<c>{MiniAppUrl}/tg</c>) via the
/// Bot API (<c>setChatMenuButton</c>). This makes the Mini App the bot's default
/// "Open App" surface without anyone touching @BotFather — the operator only sets
/// the bot token + Mini App URL in server config.
///
/// Runs as a background task so it never blocks host startup, and is fully
/// fire-and-forget: if the token is missing, the URL isn't https, or Telegram is
/// unreachable, it logs and moves on. Idempotent — safe to re-run every boot.
/// </summary>
internal sealed class TelegramMenuButtonHostedService : BackgroundService
{
    private const string HttpClientName = "Telegram";
    private const string ApiBase = "https://api.telegram.org";

    private readonly IHttpClientFactory _httpFactory;
    private readonly TelegramOptions _options;
    private readonly ILogger<TelegramMenuButtonHostedService> _logger;

    public TelegramMenuButtonHostedService(
        IHttpClientFactory httpFactory,
        IOptions<TelegramOptions> options,
        ILogger<TelegramMenuButtonHostedService> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var token = _options.BotToken;
        var miniApp = _options.MiniAppUrl?.Trim().TrimEnd('/');

        // Nothing to configure (local/dev without a token or URL) — no-op, no noise.
        // A real bot token is "<id>:<secret>"; skip obvious placeholders so dev
        // boots clean instead of logging a confusing 404 from api.telegram.org.
        if (string.IsNullOrWhiteSpace(token) || !token.Contains(':') || string.IsNullOrWhiteSpace(miniApp))
            return;

        // The Mini App entry page is /tg. Telegram only accepts https Web App URLs,
        // so skip http/localhost so dev boots clean instead of erroring.
        var webAppUrl = $"{miniApp}/tg";
        if (!webAppUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "Telegram menu button not set — Mini App URL must be https (got {Url}).", webAppUrl);
            return;
        }

        try
        {
            var http = _httpFactory.CreateClient(HttpClientName);
            var url = $"{ApiBase}/bot{token}/setChatMenuButton";
            var payload = new
            {
                menu_button = new
                {
                    type = "web_app",
                    text = "Open App",
                    web_app = new { url = webAppUrl },
                },
            };

            using var response = await http.PostAsJsonAsync(url, payload, stoppingToken);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Telegram default menu button set to Mini App {Url}.", webAppUrl);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync(stoppingToken);
                _logger.LogWarning(
                    "Telegram setChatMenuButton failed ({Status}): {Body}", (int)response.StatusCode, body);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Host shutting down before the call finished — ignore.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Telegram setChatMenuButton threw — menu button not set.");
        }
    }
}
