using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using EventForge.Api.Authentication;
using EventForge.Api.Data;
using EventForge.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace EventForge.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    IMongoRepository<UserDocument> users,
    JwtTokenService tokenService) : ControllerBase
{
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        if (!new EmailAddressAttribute().IsValid(email))
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid email address.");
        }

        if (request.Password.Length < 12)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Password must contain at least 12 characters.");
        }

        if (request.DisplayName.Trim().Length is < 2 or > 80)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Display name must contain 2 to 80 characters.");
        }

        var existing = await users.FindOneAsync(
            Builders<UserDocument>.Filter.Eq(user => user.Email, email),
            cancellationToken);
        if (existing is not null)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Email is already registered.",
                Status = StatusCodes.Status409Conflict
            });
        }

        var user = await users.InsertAsync(new UserDocument
        {
            Email = email,
            DisplayName = request.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = RoleNames.Attendee
        }, cancellationToken);

        return CreatedAtAction(nameof(Me), CreateResponse(user));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await users.FindOneAsync(
            Builders<UserDocument>.Filter.Eq(candidate => candidate.Email, email),
            cancellationToken);

        if (user is null || !user.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid email or password.",
                Status = StatusCodes.Status401Unauthorized
            });
        }

        return Ok(CreateResponse(user));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserProfile>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized();
        }

        var user = await users.FindByIdAsync(userId, cancellationToken);
        return user is null ? NotFound() : Ok(user.ToProfile());
    }

    private AuthResponse CreateResponse(UserDocument user)
    {
        var token = tokenService.CreateAccessToken(user);
        return new AuthResponse(token.Token, token.ExpiresAtUtc, user.ToProfile());
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
