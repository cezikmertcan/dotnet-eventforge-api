using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EventForge.Api.Authentication;
using EventForge.Api.Configuration;
using EventForge.Api.Models;
using Microsoft.Extensions.Options;

namespace EventForge.Api.Tests;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreatesTokenWithIdentityAndRoleClaims()
    {
        var options = Options.Create(new JwtOptions
        {
            Issuer = "EventForge.Tests",
            Audience = "EventForge.TestClient",
            SigningKey = "test-signing-key-that-is-at-least-32-characters-long",
            AccessTokenMinutes = 15
        });
        var user = new UserDocument
        {
            Id = "507f1f77bcf86cd799439011",
            Email = "organizer@example.com",
            DisplayName = "Test Organizer",
            Role = RoleNames.Organizer,
            PasswordHash = "not-used"
        };

        var result = new JwtTokenService(options).CreateAccessToken(user);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal(options.Value.Issuer, token.Issuer);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.NameIdentifier && claim.Value == user.Id);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Email && claim.Value == user.Email);
        Assert.Contains(token.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == RoleNames.Organizer);
        Assert.True(result.ExpiresAtUtc > DateTime.UtcNow);
    }
}
