using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using LMS.Application.Common.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Verifies Telegram Mini App initData per Telegram's spec:
///   secret_key = HMAC_SHA256(key="WebAppData", data=bot_token)
///   hash       = hex(HMAC_SHA256(key=secret_key, data=data_check_string))
/// where data_check_string is every field except "hash"/"signature", sorted by
/// key, "key=value" joined by '\n'. Also enforces an auth_date freshness window
/// (replay/expiry protection). The bot token is the shared secret, so a valid
/// hash proves the payload was issued by Telegram for THIS bot and untampered.
/// </summary>
public sealed class TelegramInitDataValidator(
    IOptions<TelegramOptions> options,
    ILogger<TelegramInitDataValidator> logger) : ITelegramInitDataValidator
{
    // initData is reused while the Mini App stays open; 24h is a safe upper
    // bound. The real session is the short-lived platform JWT we issue after.
    private static readonly TimeSpan MaxAge = TimeSpan.FromHours(24);

    public (TelegramInitData? User, string? Error) Validate(string initData)
    {
        var botToken = options.Value.BotToken;
        if (string.IsNullOrWhiteSpace(botToken))
            return (null, "Telegram bot token is not configured.");
        if (string.IsNullOrWhiteSpace(initData))
            return (null, "initData is empty.");

        // Parse the x-www-form-urlencoded payload into decoded key/value pairs.
        var pairs = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var part in initData.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var eq = part.IndexOf('=');
            if (eq <= 0) continue;
            var key = Uri.UnescapeDataString(part[..eq]);
            var value = Uri.UnescapeDataString(part[(eq + 1)..]);
            pairs[key] = value;
        }

        if (!pairs.TryGetValue("hash", out var providedHash) || string.IsNullOrEmpty(providedHash))
            return (null, "Missing hash.");

        byte[] providedBytes;
        try { providedBytes = Convert.FromHexString(providedHash); }
        catch { return (null, "Malformed hash."); }

        var secretKey = HmacSha256(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes(botToken));

        // Telegram clients disagree on whether the newer `signature` field belongs
        // in the data_check_string (some include it in the HMAC, some don't). Accept
        // EITHER convention — the HMAC is still fully verified with the bot token,
        // so this only tolerates the client difference; it does not weaken the check.
        // When no `signature` field is present, both variants are identical.
        if (!HashMatches(pairs, secretKey, providedBytes, excludeSignature: true) &&
            !HashMatches(pairs, secretKey, providedBytes, excludeSignature: false))
        {
            // Diagnostic (keys only — no PII): if this fires the token is right but
            // the data_check_string differs (unexpected field / encoding).
            logger.LogWarning(
                "Telegram initData signature mismatch. Fields present: [{Keys}]",
                string.Join(',', pairs.Keys.OrderBy(k => k, StringComparer.Ordinal)));
            return (null, "Invalid signature.");
        }

        // Freshness — reject stale payloads (replay/expiry).
        if (!pairs.TryGetValue("auth_date", out var authDateRaw) ||
            !long.TryParse(authDateRaw, out var authUnix))
            return (null, "Missing auth_date.");
        var authDate = DateTimeOffset.FromUnixTimeSeconds(authUnix);
        if (DateTimeOffset.UtcNow - authDate > MaxAge)
            return (null, "initData has expired.");
        if (authDate - DateTimeOffset.UtcNow > TimeSpan.FromMinutes(5))
            return (null, "initData auth_date is in the future.");

        if (!pairs.TryGetValue("user", out var userJson) || string.IsNullOrWhiteSpace(userJson))
            return (null, "Missing user.");

        TgUser? u;
        try { u = JsonSerializer.Deserialize<TgUser>(userJson); }
        catch { return (null, "Malformed user."); }
        if (u is null || u.id <= 0) return (null, "Invalid user.");

        return (new TelegramInitData
        {
            UserId = u.id,
            Username = u.username,
            FirstName = u.first_name,
            LastName = u.last_name,
            PhotoUrl = u.photo_url,
        }, null);
    }

    /// <summary>
    /// Builds the data_check_string (all fields except <c>hash</c>, and optionally
    /// except <c>signature</c>), computes the HMAC, and constant-time compares it
    /// to the provided hash.
    /// </summary>
    private static bool HashMatches(
        IReadOnlyDictionary<string, string> pairs, byte[] secretKey, byte[] providedBytes, bool excludeSignature)
    {
        var dataCheckString = string.Join('\n', pairs
            .Where(kv => kv.Key != "hash" && (!excludeSignature || kv.Key != "signature"))
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => $"{kv.Key}={kv.Value}"));
        var computed = HmacSha256(secretKey, Encoding.UTF8.GetBytes(dataCheckString));
        return CryptographicOperations.FixedTimeEquals(computed, providedBytes);
    }

    private static byte[] HmacSha256(byte[] key, byte[] data)
    {
        using var h = new HMACSHA256(key);
        return h.ComputeHash(data);
    }

    // Matches Telegram's user object inside initData (snake_case).
    private sealed class TgUser
    {
        public long id { get; set; }
        public string? username { get; set; }
        public string? first_name { get; set; }
        public string? last_name { get; set; }
        public string? photo_url { get; set; }
    }
}
