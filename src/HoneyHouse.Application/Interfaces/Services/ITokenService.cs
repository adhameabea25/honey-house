using HoneyHouse.Domain.Entities;

namespace HoneyHouse.Application.Interfaces.Services;

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(ApplicationUser user, IEnumerable<string> roles);
    /// <summary>Returns a new random refresh token (to give to the client) and its expiry.</summary>
    (string Token, DateTime ExpiresAt) CreateRefreshToken();
    string HashRefreshToken(string token);
}

public interface IEmailSender
{
    Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default);
}
