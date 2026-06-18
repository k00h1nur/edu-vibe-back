using LMS.Application.Common.Models;
using LMS.Application.Features.Auth;
using MediatR;

namespace LMS.Application.Features.Telegram;

/// <summary>
/// Verified snapshot of the user's linked Telegram identity, surfaced to the
/// web/Mini App so it can show "connected as @handle" and a disconnect CTA.
/// </summary>
public sealed record TelegramProfileDto(
    long TelegramUserId,
    string? Username,
    string? FirstName,
    string? LastName,
    string? PhotoUrl,
    DateTime LinkedAt);

/// <summary>
/// Authenticate through the Telegram Mini App. The body carries the raw,
/// signed <c>window.Telegram.WebApp.initData</c> string. The handler verifies
/// the signature, then either signs in the already-linked user or provisions a
/// fresh Student account for a first-time Telegram visitor — returning the same
/// JWT/refresh pair the email/password login issues, so every downstream
/// consumer (middleware, panels) treats a Telegram session identically.
/// </summary>
public sealed record TelegramAuthCommand(string InitData, string? StartToken = null)
    : IRequest<Result<AuthTokensResponse>>;

/// <summary>
/// Link the Telegram identity in <paramref name="InitData"/> to the
/// <em>currently authenticated</em> web user (taken from the JWT, never the
/// body). Used by the "Connect Telegram" button inside the panels. Fails if the
/// Telegram account is already attached to a different user.
/// </summary>
public sealed record TelegramLinkCommand(string InitData) : IRequest<Result<TelegramProfileDto>>;

/// <summary>Read the current user's linked Telegram profile, or null if none.</summary>
public sealed record GetTelegramProfileQuery : IRequest<Result<TelegramProfileDto?>>;

/// <summary>Disconnect the current user's Telegram link (idempotent).</summary>
public sealed record UnlinkTelegramCommand : IRequest<Result>;

// ----- Platform bot settings (singleton) ----------------------------------

/// <summary>
/// Public, safe-to-expose bot settings sourced from server config (the bot is
/// fixed by the operator — not editable from the admin UI). Secrets (token,
/// chat id) stay server-side.
/// </summary>
public sealed record TelegramSettingsDto(string? BotUsername, string? MiniAppUrl, DateTime UpdatedAt);

/// <summary>Reads the configured bot settings (bot @username + Mini App URL) from server config.</summary>
public sealed record GetTelegramSettingsQuery : IRequest<Result<TelegramSettingsDto>>;

// ----- Deep-link session handoff ------------------------------------------

/// <summary>The minted handoff token + ready-to-open Telegram Mini App deep link.</summary>
public sealed record DeepLinkTokenDto(string Token, string DeepLink, DateTime ExpiresAt);

/// <summary>
/// Mints a short-lived, one-time handoff token for the CURRENT signed-in user
/// (taken from the JWT). The panel opens the returned <c>DeepLink</c>, and the
/// Mini App exchanges the token (via <see cref="TelegramAuthCommand.StartToken"/>)
/// to sign in as that same user.
/// </summary>
public sealed record CreateDeepLinkTokenCommand : IRequest<Result<DeepLinkTokenDto>>;
