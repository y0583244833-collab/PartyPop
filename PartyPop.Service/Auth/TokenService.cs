using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PartyPop.Core.Entities;
using PartyPop.Core.Interfaces.Services;
using PartyPop.Core.Settings;

namespace PartyPop.Service.Auth;

/// <summary>יוצר JWT עם Claims: sub, email, name, role.</summary>
public class TokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly TimeProvider _time;

    public TokenService(IOptions<JwtSettings> settings, TimeProvider time)
    {
        _settings = settings.Value;
        _time = time;
    }

    public (string Token, DateTime ExpiresAt) CreateToken(User user)
    {
        var now = _time.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(_settings.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim("name", $"{user.FirstName} {user.LastName}"),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
