using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Infrastructure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace HoneyHouse.UnitTests.Application;

public class JwtTokenServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

    private static JwtTokenService Create(string key = "unit-test-signing-key-0123456789abcdef") =>
        new(Options.Create(new JwtOptions { SigningKey = key, AccessTokenMinutes = 15, RefreshTokenDays = 7 }), new FakeTimeProvider(Now));

    [Fact]
    public void Access_token_contains_user_and_role_claims()
    {
        var user = new ApplicationUser { Id = "u1", Email = "a@b.com", FullName = "Ali", PreferredLanguage = "ar" };
        var (token, expires) = Create().CreateAccessToken(user, ["Customer", "Manager"]);

        expires.ShouldBe(Now.UtcDateTime.AddMinutes(15));
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        jwt.Subject.ShouldBe("u1");
        jwt.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ShouldBe(["Customer", "Manager"]);
        jwt.Claims.Single(c => c.Type == "lang").Value.ShouldBe("ar");
    }

    [Fact]
    public void Refresh_tokens_are_random_and_hash_is_stable()
    {
        var svc = Create();
        var (a, expA) = svc.CreateRefreshToken();
        var (b, _) = svc.CreateRefreshToken();
        a.ShouldNotBe(b);
        expA.ShouldBe(Now.UtcDateTime.AddDays(7));
        svc.HashRefreshToken(a).ShouldBe(svc.HashRefreshToken(a));
        svc.HashRefreshToken(a).ShouldNotBe(a);
    }

    [Fact]
    public void Short_signing_key_is_rejected() =>
        Should.Throw<InvalidOperationException>(() => Create("too-short").CreateAccessToken(new ApplicationUser { Id = "x" }, []));
}
