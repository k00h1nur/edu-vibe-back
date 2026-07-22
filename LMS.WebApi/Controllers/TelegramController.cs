using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LMS.Application.Features.Auth;
using LMS.Application.Features.Telegram;
using LMS.Infrastructure.Services;
using LMS.WebApi.Common;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace LMS.WebApi.Controllers;

/// <summary>
/// Telegram Mini App auth surface. <c>/auth</c> is the only anonymous endpoint —
/// it trades a signed initData for the same JWT/refresh pair as email login.
/// The link/profile/unlink endpoints operate on the authenticated web user so a
/// signed-in person can connect or disconnect their Telegram from the panels.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class TelegramController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Authenticate with <c>window.Telegram.WebApp.initData</c>. Signs in the
    /// linked user or auto-provisions a Student on first contact. Anonymous +
    /// rate-limited (shares the auth throttle) since it mints a session.
    /// </summary>
    [HttpPost("auth")]
    [AllowAnonymous]
    [EnableRateLimiting("auth-anon")]
    public async Task<ActionResult<ApiResponse<AuthTokensResponse>>> Auth(
        [FromBody] TelegramAuthCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.Success)
            return Unauthorized(ApiResponse<AuthTokensResponse>.Fail(result.Message ?? "Telegram auth failed"));
        return Ok(ApiResponse<AuthTokensResponse>.Ok(result.Data, result.Message));
    }

    /// <summary>Connect the verified Telegram identity to the signed-in user.</summary>
    [HttpPost("link")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TelegramProfileDto>>> Link(
        [FromBody] TelegramLinkCommand command, CancellationToken cancellationToken)
    {
        var result = await sender.Send(command, cancellationToken);
        if (!result.Success)
            return BadRequest(ApiResponse<TelegramProfileDto>.Fail(result.Message ?? "Telegram link failed"));
        return Ok(ApiResponse<TelegramProfileDto>.Ok(result.Data, result.Message));
    }

    /// <summary>The signed-in user's linked Telegram profile (null if none).</summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<TelegramProfileDto?>>> Profile(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTelegramProfileQuery(), cancellationToken);
        if (!result.Success)
            return BadRequest(ApiResponse<TelegramProfileDto?>.Fail(result.Message ?? "Failed to load profile"));
        return Ok(ApiResponse<TelegramProfileDto?>.Ok(result.Data, result.Message));
    }

    /// <summary>Disconnect the signed-in user's Telegram link (idempotent).</summary>
    [HttpDelete("link")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> Unlink(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new UnlinkTelegramCommand(), cancellationToken);
        if (!result.Success)
            return BadRequest(ApiResponse<object>.Fail(result.Message ?? "Telegram unlink failed"));
        return Ok(ApiResponse<object>.Ok(new { }, result.Message));
    }

    /// <summary>
    /// Mints a one-time deep-link handoff token for the signed-in user and
    /// returns the Telegram Mini App deep link to open. The Mini App then signs
    /// the same user in (no password) via the token.
    /// </summary>
    [HttpPost("deep-link")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<DeepLinkTokenDto>>> CreateDeepLink(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateDeepLinkTokenCommand(), cancellationToken);
        if (!result.Success)
            return BadRequest(ApiResponse<DeepLinkTokenDto>.Fail(result.Message ?? "Couldn't create deep link"));
        return Ok(ApiResponse<DeepLinkTokenDto>.Ok(result.Data, result.Message));
    }

    // ----- Platform bot settings ------------------------------------------

    /// <summary>
    /// Public bot settings (bot @username + Mini App URL) so any panel can build
    /// the "Open in Telegram" deep link. Sourced from server config — the bot is
    /// fixed by the operator and cannot be changed from the UI (no upsert). Anonymous.
    /// </summary>
    [HttpGet("settings")]
    [AllowAnonymous]
    public async Task<ActionResult<ApiResponse<TelegramSettingsDto>>> GetSettings(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTelegramSettingsQuery(), cancellationToken);
        return Ok(ApiResponse<TelegramSettingsDto>.Ok(result.Data, result.Message));
    }

    // ----- Bot webhook (/start welcome) -----------------------------------

    /// <summary>
    /// Telegram Bot API webhook. Anonymous (Telegram calls it directly) but gated
    /// by the shared <c>Telegram:WebhookSecret</c> echoed in the
    /// <c>X-Telegram-Bot-Api-Secret-Token</c> header — so it fails closed until a
    /// secret is configured. On <c>/start</c> we answer via Telegram's "respond
    /// with a method" convention: a localized welcome plus an inline keyboard whose
    /// primary button opens the marketing site (edu-vibe.uz) and, for students, the
    /// Mini App. Every other update is acknowledged (200) and ignored.
    /// </summary>
    [HttpPost("webhook")]
    [AllowAnonymous]
    public IActionResult Webhook(
        [FromBody] TgUpdate update,
        [FromServices] IOptions<TelegramOptions> options)
    {
        var opts = options.Value;

        var provided = Request.Headers["X-Telegram-Bot-Api-Secret-Token"].ToString();
        if (string.IsNullOrEmpty(opts.WebhookSecret) || !SecretMatches(provided, opts.WebhookSecret))
            return Unauthorized();

        var msg = update.Message;
        var text = msg?.Text?.Trim();
        var chatId = msg?.Chat?.Id;

        var isStart = text is not null &&
            (text == "/start" || text.StartsWith("/start ", StringComparison.Ordinal));
        if (chatId is null || !isStart)
            return Ok();

        var reply = BuildStartReply(chatId.Value, NormalizeLang(msg!.From?.LanguageCode), opts);
        // Serialize ourselves so the raw snake_case Bot API keys survive regardless
        // of the JSON naming policy MVC is configured with.
        return Content(JsonSerializer.Serialize(reply), "application/json");
    }

    /// <summary>Constant-time compare of the webhook secret header.</summary>
    private static bool SecretMatches(string provided, string expected)
    {
        if (string.IsNullOrEmpty(provided)) return false;
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(provided), Encoding.UTF8.GetBytes(expected));
    }

    /// <summary>Telegram <c>language_code</c> → one of our three supported locales.</summary>
    private static string NormalizeLang(string? code)
    {
        if (string.IsNullOrEmpty(code)) return "en";
        if (code.StartsWith("uz", StringComparison.OrdinalIgnoreCase)) return "uz";
        if (code.StartsWith("ru", StringComparison.OrdinalIgnoreCase)) return "ru";
        return "en";
    }

    private const string WelcomeUz =
        "<b>EduVibe'ga xush kelibsiz!</b> 📚\n\n" +
        "Aniqlik bilan o'rganing, ishonch bilan natija qiling — IELTS, SAT va umumiy ingliz tili.\n\n" +
        "🎯 <b>Yangimisiz?</b> Saytimizdan bepul demo darsga yoziling.\n" +
        "🎓 <b>O'quvchimisiz?</b> Ilovadan darslar, vazifalar va baholaringizni ko'ring.";

    private const string WelcomeRu =
        "<b>Добро пожаловать в EduVibe!</b> 📚\n\n" +
        "Учитесь с ясностью, достигайте с уверенностью — IELTS, SAT и общий английский.\n\n" +
        "🎯 <b>Впервые здесь?</b> Запишитесь на бесплатный демо-урок на сайте.\n" +
        "🎓 <b>Уже учитесь?</b> Откройте приложение — уроки, задания и оценки.";

    private const string WelcomeEn =
        "<b>Welcome to EduVibe!</b> 📚\n\n" +
        "Learn with clarity, perform with confidence — IELTS, SAT and general English.\n\n" +
        "🎯 <b>New here?</b> Book a free demo lesson on our site.\n" +
        "🎓 <b>Already a student?</b> Open the app for your lessons, homework and grades.";

    private static Dictionary<string, object?> BuildStartReply(long chatId, string lang, TelegramOptions opts)
    {
        var (body, siteLabel, appLabel) = lang switch
        {
            "uz" => (WelcomeUz, "🌐 Sayt — edu-vibe.uz", "📚 Ilovani ochish"),
            "ru" => (WelcomeRu, "🌐 Сайт — edu-vibe.uz", "📚 Открыть приложение"),
            _ => (WelcomeEn, "🌐 Website — edu-vibe.uz", "📚 Open the app"),
        };

        var rows = new List<List<Dictionary<string, object?>>>();

        // Primary CTA — the marketing site (always present).
        var website = string.IsNullOrWhiteSpace(opts.WebsiteUrl) ? "https://edu-vibe.uz" : opts.WebsiteUrl.Trim();
        rows.Add([new() { ["text"] = siteLabel, ["url"] = website }]);

        // Students — open the Mini App, only when a real https Mini App URL is set.
        var miniApp = opts.MiniAppUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(miniApp) &&
            miniApp.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            rows.Add([new()
            {
                ["text"] = appLabel,
                ["web_app"] = new Dictionary<string, object?> { ["url"] = $"{miniApp}/tg" },
            }]);
        }

        return new Dictionary<string, object?>
        {
            ["method"] = "sendMessage",
            ["chat_id"] = chatId,
            ["text"] = body,
            ["parse_mode"] = "HTML",
            ["disable_web_page_preview"] = true,
            ["reply_markup"] = new Dictionary<string, object?> { ["inline_keyboard"] = rows },
        };
    }
}

// ---- Minimal Bot API update shape — only the fields /start needs. ----------
public sealed record TgUpdate([property: JsonPropertyName("message")] TgMessage? Message);

public sealed record TgMessage(
    [property: JsonPropertyName("text")] string? Text,
    [property: JsonPropertyName("chat")] TgChat? Chat,
    [property: JsonPropertyName("from")] TgFrom? From);

public sealed record TgChat([property: JsonPropertyName("id")] long Id);

public sealed record TgFrom([property: JsonPropertyName("language_code")] string? LanguageCode);
