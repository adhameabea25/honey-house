namespace HoneyHouse.Application.Features.Auth;

public sealed record RegisterRequest(string FullName, string Email, string PhoneNumber, string Password, string? PreferredLanguage = null);
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record UpdateProfileRequest(string FullName, string? PhoneNumber, string? PreferredLanguage);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record UserDto(string Id, string FullName, string Email, string? PhoneNumber, string PreferredLanguage, IReadOnlyList<string> Roles);

public sealed record AuthResponse(
    string AccessToken,
    DateTime AccessTokenExpiresAt,
    string RefreshToken,
    DateTime RefreshTokenExpiresAt,
    UserDto User);
