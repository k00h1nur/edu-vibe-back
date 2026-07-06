using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using LMS.Application.Common.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Signs short-lived access tokens. Registered as a singleton — the signing
/// key is loaded once at construction so each token issuance only does the
/// claim marshalling, not config re-reads + key allocation.
/// </summary>
public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly SigningCredentials _signingCredentials;
    private readonly string? _issuer;
    private readonly string? _audience;
    private readonly TimeSpan _accessTokenLifetime;
    private readonly JwtSecurityTokenHandler _handler = new();

    public JwtTokenGenerator(IOptions<JwtOptions> options)
    {
        var jwt = options.Value;
        // Validates presence + ≥32-byte length in the one shared place (JwtOptions).
        var keyBytes = jwt.SigningKeyBytes();

        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
        _issuer = jwt.Issuer;
        _audience = jwt.Audience;
        _accessTokenLifetime = jwt.AccessTokenLifetime;
        RefreshTokenLifetime = jwt.RefreshTokenLifetime;
    }

    public TimeSpan RefreshTokenLifetime { get; }

    public string Generate(
        Guid userId,
        string email,
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        Guid? studentProfileId = null,
        Guid? staffProfileId = null)
    {
        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        if (studentProfileId.HasValue)
            claims.Add(new Claim("studentProfileId", studentProfileId.Value.ToString()));
        if (staffProfileId.HasValue)
            claims.Add(new Claim("staffProfileId", staffProfileId.Value.ToString()));

        var token = new JwtSecurityToken(
            _issuer,
            _audience,
            claims,
            expires: DateTime.UtcNow.Add(_accessTokenLifetime),
            signingCredentials: _signingCredentials);

        return _handler.WriteToken(token);
    }
}
