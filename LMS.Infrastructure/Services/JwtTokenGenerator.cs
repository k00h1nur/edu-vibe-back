using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LMS.Application.Common.Abstractions;
using Microsoft.Extensions.Configuration;
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

    public JwtTokenGenerator(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException(
                "Jwt:Key is required. Set it via configuration or the Jwt__Key environment variable.");

        var keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "Jwt:Key must be at least 32 bytes for HS256.");
        }

        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);
        _issuer = configuration["Jwt:Issuer"];
        _audience = configuration["Jwt:Audience"];
        // Default 30 min — short enough that a stolen token is mostly stale, long
        // enough that legitimate users don't refresh every other request.
        // Refresh tokens live for 7 days, so silent renewal handles the rest.
        _accessTokenLifetime = TimeSpan.FromMinutes(
            configuration.GetValue("Jwt:AccessTokenMinutes", 30));
    }

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
