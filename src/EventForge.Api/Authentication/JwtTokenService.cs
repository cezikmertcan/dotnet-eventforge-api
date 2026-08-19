using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EventForge.Api.Configuration;
using EventForge.Api.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EventForge.Api.Authentication;

public sealed class JwtTokenService(IOptions<JwtOptions> options)
{
    public (string Token, DateTime ExpiresAtUtc) CreateAccessToken(UserDocument user)
    {
        var settings = options.Value;
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(settings.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            expires: expiresAtUtc,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
