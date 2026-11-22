namespace HoneyHouse.Infrastructure.Identity;

public sealed class JwtOptions
{
    public const string Section = "Jwt";
    public string Issuer { get; set; } = "HoneyHouse";
    public string Audience { get; set; } = "HoneyHouse.Clients";
    /// <summary>HMAC-SHA256 key, at least 32 characters. Must come from secrets in production.</summary>
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 7;
}
