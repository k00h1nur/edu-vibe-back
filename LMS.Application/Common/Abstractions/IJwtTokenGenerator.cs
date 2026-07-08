namespace LMS.Application.Common.Abstractions;

public interface IJwtTokenGenerator
{
    /// <summary>
    /// How long a freshly-issued refresh token stays valid. Sourced from the
    /// Jwt config (RefreshTokenDays, default 30) so the TTL lives in one place
    /// instead of being hardcoded at every issue site.
    /// </summary>
    TimeSpan RefreshTokenLifetime { get; }

    /// <summary>
    /// Issues a signed JWT for the supplied user.
    /// </summary>
    /// <param name="userId">The user's primary id.</param>
    /// <param name="email">Email claim.</param>
    /// <param name="roles">Role codes to embed as <see cref="System.Security.Claims.ClaimTypes.Role"/> claims.</param>
    /// <param name="permissions">Permission codes to embed as <c>permission</c> claims.</param>
    /// <param name="studentProfileId">Optional student profile id, embedded as <c>studentProfileId</c>.</param>
    /// <param name="staffProfileId">Optional staff profile id, embedded as <c>staffProfileId</c>.</param>
    string Generate(
        Guid userId,
        string email,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        Guid? studentProfileId = null,
        Guid? staffProfileId = null);
}
