using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LMS.Application.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LMS.Infrastructure.Services;

public sealed class JwtTokenGenerator(IConfiguration configuration) : IJwtTokenGenerator
{
    private const string FallbackJwtKey = "EduVibe-Fallback-Secret-Key-At-Least-32-Chars";

    public string Generate(Guid userId, string email, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        var claims = new List<Claim>
        {
            new("userId", userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email)
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => new Claim("permission", p)));

        var configuredKey = configuration["Jwt:Key"];
        var jwtKey = string.IsNullOrWhiteSpace(configuredKey) ? FallbackJwtKey : configuredKey;
        var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
        if (keyBytes.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT key must be at least 32 bytes for HS256. Update Jwt:Key in configuration.");
        }

        var key = new SymmetricSecurityKey(keyBytes);
        var token = new JwtSecurityToken(
            configuration["Jwt:Issuer"],
            configuration["Jwt:Audience"],
            claims,
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
