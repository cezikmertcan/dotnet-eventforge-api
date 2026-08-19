using EventForge.Api.Models;

namespace EventForge.Api.Authentication;

public sealed class RegisterRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;
}

public sealed class LoginRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;
}

public sealed class RoleUpdateRequest
{
    public string Role { get; init; } = string.Empty;
}

public sealed record UserProfile(
    string Id,
    string Email,
    string DisplayName,
    string Role,
    bool IsActive);

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    UserProfile User);

public static class UserProfileMapper
{
    public static UserProfile ToProfile(this UserDocument user)
        => new(user.Id, user.Email, user.DisplayName, user.Role, user.IsActive);
}
