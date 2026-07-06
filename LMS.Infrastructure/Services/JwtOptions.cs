using System.Text;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Strongly-typed binding for the "Jwt" section of appsettings / env. Read once
/// at startup and injected via <c>IOptions&lt;JwtOptions&gt;</c>, replacing the
/// scattered <c>IConfiguration["Jwt:*"]</c> indexer reads that used to live in
/// both the bearer setup and the token generator.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>HS256 signing key. Must be ≥ 32 bytes. Required in Production.</summary>
    public string? Key { get; init; }

    public string? Issuer { get; init; }

    public string? Audience { get; init; }

    /// <summary>
    /// Access-token lifetime, minutes. Default 30 — short enough that a stolen
    /// token is mostly stale, long enough that legitimate users don't refresh
    /// every other request. Silent renewal via the refresh token handles the rest.
    /// </summary>
    public int AccessTokenMinutes { get; init; } = 30;

    /// <summary>Refresh-token lifetime, days. Default 30.</summary>
    public int RefreshTokenDays { get; init; } = 30;

    public TimeSpan AccessTokenLifetime => TimeSpan.FromMinutes(AccessTokenMinutes);

    public TimeSpan RefreshTokenLifetime => TimeSpan.FromDays(RefreshTokenDays);

    /// <summary>
    /// Returns the HS256 signing-key bytes, validating presence + minimum length
    /// in ONE place. Both <c>AddJwtAuthentication</c> and <c>JwtTokenGenerator</c>
    /// call this, so the "key is set and long enough" check is no longer duplicated.
    /// </summary>
    public byte[] SigningKeyBytes()
    {
        var key = Key
            ?? throw new InvalidOperationException(
                "Jwt:Key is required. Set it via configuration or the Jwt__Key environment variable.");

        var bytes = Encoding.UTF8.GetBytes(key);
        if (bytes.Length < 32)
            throw new InvalidOperationException("Jwt:Key must be at least 32 bytes for HS256.");

        return bytes;
    }
}
