using HoneyHouse.API.Common;
using HoneyHouse.API.Extensions;
using HoneyHouse.Application.Features.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HoneyHouse.API.Controllers;

/// <summary>Registration, sign-in, token refresh and profile.</summary>
[Route("api/v1/auth")]
public class AuthController(IAuthService auth) : ApiControllerBase
{
    /// <summary>Create a customer account and sign in.</summary>
    [HttpPost("register")]
    [EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct) =>
        FromResult(await auth.RegisterAsync(request, ct));

    /// <summary>Sign in with email and password. Returns an access token (15 min) and a refresh token (7 days).</summary>
    [HttpPost("login")]
    [EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct) =>
        FromResult(await auth.LoginAsync(request, ct));

    /// <summary>Exchange a refresh token for a new token pair (the old refresh token is revoked).</summary>
    [HttpPost("refresh")]
    [EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType<AuthResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken ct) =>
        FromResult(await auth.RefreshAsync(request.RefreshToken, ct));

    /// <summary>Revoke the given refresh token.</summary>
    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(RefreshTokenRequest request, CancellationToken ct) =>
        FromResult(await auth.LogoutAsync(User.GetUserId(), request.RefreshToken, ct));

    /// <summary>Send a password reset code by email. Always returns 204.</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        await auth.ForgotPasswordAsync(request, ct);
        return NoContent();
    }

    /// <summary>Set a new password using the emailed reset code.</summary>
    [HttpPost("reset-password")]
    [EnableRateLimiting(ServiceCollectionExtensions.AuthRateLimitPolicy)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken ct) =>
        FromResult(await auth.ResetPasswordAsync(request, ct));

    /// <summary>Current user's profile.</summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Me() => FromResult(await auth.GetProfileAsync(User.GetUserId()));

    /// <summary>Update name, phone and preferred language.</summary>
    [Authorize]
    [HttpPut("me")]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateMe(UpdateProfileRequest request) =>
        FromResult(await auth.UpdateProfileAsync(User.GetUserId(), request));

    /// <summary>Change password. Signs out all other sessions.</summary>
    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct) =>
        FromResult(await auth.ChangePasswordAsync(User.GetUserId(), request, ct));
}
