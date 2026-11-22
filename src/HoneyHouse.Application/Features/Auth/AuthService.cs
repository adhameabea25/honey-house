using HoneyHouse.Application.Common;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace HoneyHouse.Application.Features.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task<Result> LogoutAsync(string userId, string refreshToken, CancellationToken ct = default);
    Task<Result<UserDto>> GetProfileAsync(string userId);
    Task<Result<UserDto>> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}

public sealed class AuthService(
    UserManager<ApplicationUser> users,
    ITokenService tokens,
    IUnitOfWork uow,
    IEmailSender email,
    ILanguageContext language,
    TimeProvider clock,
    ILogger<AuthService> logger) : IAuthService
{
    private IGenericRepository<RefreshToken> RefreshTokens => uow.Repository<RefreshToken>();
    private DateTime Now => clock.GetUtcNow().UtcDateTime;

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var emailAddress = request.Email.Trim().ToLowerInvariant();
        if (await users.FindByEmailAsync(emailAddress) is not null)
            return Error.Conflict("email_taken");

        var user = new ApplicationUser
        {
            UserName = emailAddress,
            Email = emailAddress,
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber,
            PreferredLanguage = request.PreferredLanguage ?? language.Language
        };

        var created = await users.CreateAsync(user, request.Password);
        if (!created.Succeeded) return IdentityError(created);

        await users.AddToRoleAsync(user, Roles.Customer);
        return await IssueTokensAsync(user, ct);
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null) return Error.Unauthorized("invalid_credentials");
        if (!user.IsActive) return Error.Forbidden("account_disabled");
        if (await users.IsLockedOutAsync(user)) return Error.Forbidden("account_locked");

        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            return await users.IsLockedOutAsync(user)
                ? Error.Forbidden("account_locked")
                : Error.Unauthorized("invalid_credentials");
        }

        await users.ResetAccessFailedCountAsync(user);
        return await IssueTokensAsync(user, ct);
    }

    public async Task<Result<AuthResponse>> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored is null) return Error.Unauthorized("invalid_refresh_token");

        if (stored.RevokedAt is not null)
        {
            // A revoked token being reused means it was probably stolen: revoke the whole session family.
            logger.LogWarning("Refresh token reuse detected for user {UserId}", stored.UserId);
            await RevokeAllAsync(stored.UserId, ct);
            return Error.Unauthorized("invalid_refresh_token");
        }
        if (stored.ExpiresAt <= Now) return Error.Unauthorized("invalid_refresh_token");

        var user = await users.FindByIdAsync(stored.UserId);
        if (user is null || !user.IsActive) return Error.Unauthorized("invalid_refresh_token");

        var (newToken, newExpiry) = tokens.CreateRefreshToken();
        var newHash = tokens.HashRefreshToken(newToken);
        stored.RevokedAt = Now;
        stored.ReplacedByTokenHash = newHash;
        await RefreshTokens.AddAsync(new RefreshToken { UserId = user.Id, TokenHash = newHash, ExpiresAt = newExpiry }, ct);
        await uow.SaveChangesAsync(ct);

        return await BuildResponseAsync(user, newToken, newExpiry);
    }

    public async Task<Result> LogoutAsync(string userId, string refreshToken, CancellationToken ct = default)
    {
        var hash = tokens.HashRefreshToken(refreshToken);
        var stored = await RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == hash && t.UserId == userId, ct);
        if (stored is { RevokedAt: null })
        {
            stored.RevokedAt = Now;
            await uow.SaveChangesAsync(ct);
        }
        return Result.Success();
    }

    public async Task<Result<UserDto>> GetProfileAsync(string userId)
    {
        var user = await users.FindByIdAsync(userId);
        return user is null ? Error.NotFound("user_not_found") : await ToDtoAsync(user);
    }

    public async Task<Result<UserDto>> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return Error.NotFound("user_not_found");

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber;
        if (request.PreferredLanguage is not null) user.PreferredLanguage = request.PreferredLanguage;

        var updated = await users.UpdateAsync(user);
        return updated.Succeeded ? await ToDtoAsync(user) : IdentityError(updated);
    }

    public async Task<Result> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return Error.NotFound("user_not_found");

        var changed = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!changed.Succeeded)
            return changed.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch))
                ? Error.Validation("password_incorrect", "currentPassword")
                : IdentityError(changed);

        await RevokeAllAsync(user.Id, ct); // sign out other devices
        return Result.Success();
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        // Always succeeds from the caller's view so that registered emails cannot be discovered.
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null || !user.IsActive) return;

        var token = await users.GeneratePasswordResetTokenAsync(user);
        var ar = user.PreferredLanguage != "en";
        await email.SendAsync(
            user.Email!,
            ar ? "إعادة تعيين كلمة المرور - بيت العسل" : "Reset your Honey House password",
            ar ? $"<p>رمز إعادة تعيين كلمة المرور:</p><p><code>{token}</code></p>"
               : $"<p>Your password reset code:</p><p><code>{token}</code></p>",
            ct);
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null) return Error.Validation("invalid_reset_token", "token");

        var reset = await users.ResetPasswordAsync(user, request.Token, request.NewPassword);
        if (!reset.Succeeded)
            return reset.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken))
                ? Error.Validation("invalid_reset_token", "token")
                : IdentityError(reset);

        await users.ResetAccessFailedCountAsync(user);
        await users.SetLockoutEndDateAsync(user, null);
        await RevokeAllAsync(user.Id, ct);
        return Result.Success();
    }

    private async Task<Result<AuthResponse>> IssueTokensAsync(ApplicationUser user, CancellationToken ct)
    {
        var (refresh, refreshExpiry) = tokens.CreateRefreshToken();
        await RefreshTokens.AddAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokens.HashRefreshToken(refresh),
            ExpiresAt = refreshExpiry
        }, ct);
        await uow.SaveChangesAsync(ct);
        return await BuildResponseAsync(user, refresh, refreshExpiry);
    }

    private async Task<AuthResponse> BuildResponseAsync(ApplicationUser user, string refresh, DateTime refreshExpiry)
    {
        var dto = await ToDtoAsync(user);
        var (access, accessExpiry) = tokens.CreateAccessToken(user, dto.Roles);
        return new AuthResponse(access, accessExpiry, refresh, refreshExpiry, dto);
    }

    private async Task RevokeAllAsync(string userId, CancellationToken ct)
    {
        var active = await RefreshTokens.ListAsync(t => t.UserId == userId && t.RevokedAt == null, ct);
        foreach (var t in active) t.RevokedAt = Now;
        await uow.SaveChangesAsync(ct);
    }

    private async Task<UserDto> ToDtoAsync(ApplicationUser user) =>
        new(user.Id, user.FullName, user.Email!, user.PhoneNumber, user.PreferredLanguage, (await users.GetRolesAsync(user)).ToList());

    private static Error IdentityError(IdentityResult result)
    {
        var first = result.Errors.First();
        return first.Code switch
        {
            nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName) => Error.Conflict("email_taken"),
            _ when first.Code.StartsWith("Password") => Error.Validation("password_weak", "password"),
            _ => Error.Business("identity_error", first.Description)
        };
    }
}
