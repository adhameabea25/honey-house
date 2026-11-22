using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using HoneyHouse.Application.Features.Auth;
using HoneyHouse.IntegrationTests.Infrastructure;
using Shouldly;

namespace HoneyHouse.IntegrationTests;

[Collection(ApiCollection.Name)]
public class AuthTests(HoneyHouseApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_returns_tokens_and_customer_role()
    {
        var email = AuthHelper.UniqueEmail();
        var auth = await _client.RegisterAsync(email);
        auth.AccessToken.ShouldNotBeNullOrWhiteSpace();
        auth.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        auth.User.Email.ShouldBe(email);
        auth.User.Roles.ShouldBe(["Customer"]);
    }

    [Fact]
    public async Task Register_with_existing_email_returns_409()
    {
        var email = AuthHelper.UniqueEmail();
        await _client.RegisterAsync(email);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("Other", email.ToUpperInvariant(), "01112345678", AuthHelper.DefaultPassword));
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("email_taken");
    }

    [Fact]
    public async Task Register_validation_errors_are_localized()
    {
        var client = factory.CreateClient().WithLanguage("ar");
        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("", "not-an-email", "123", "short"));
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var json = await response.ReadJsonAsync();
        json.GetProperty("title").GetString().ShouldBe("خطأ في التحقق من البيانات");
        var errors = json.GetProperty("errors");
        errors.GetProperty("phoneNumber")[0].GetString()!.ShouldContain("رقم الهاتف");
        errors.TryGetProperty("email", out _).ShouldBeTrue();
        errors.TryGetProperty("password", out _).ShouldBeTrue();
        errors.TryGetProperty("fullName", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Login_with_wrong_password_returns_401_in_english()
    {
        var email = AuthHelper.UniqueEmail();
        await _client.RegisterAsync(email);
        var response = await factory.CreateClient().WithLanguage("en")
            .PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Wrong12345"));
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.ReadJsonAsync()).GetProperty("title").GetString().ShouldBe("Invalid email or password.");
    }

    [Fact]
    public async Task Account_is_locked_after_five_failed_logins()
    {
        var email = AuthHelper.UniqueEmail();
        await _client.RegisterAsync(email);
        HttpResponseMessage response = null!;
        for (var i = 0; i < 5; i++)
            response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, "Wrong12345"));
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("account_locked");

        var correct = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, AuthHelper.DefaultPassword));
        correct.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Me_requires_authentication()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await response.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("unauthorized");
    }

    [Fact]
    public async Task Me_returns_profile_and_can_be_updated()
    {
        var (client, auth) = await factory.CreateCustomerClientAsync();
        var me = await client.GetFromJsonAsync<UserDto>("/api/v1/auth/me", HttpExtensions.Json);
        me!.Id.ShouldBe(auth.User.Id);

        var updated = await client.PutAsJsonAsync("/api/v1/auth/me", new UpdateProfileRequest("أحمد علي", "01512345678", "en"));
        updated.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await updated.Content.ReadFromJsonAsync<UserDto>(HttpExtensions.Json);
        dto!.FullName.ShouldBe("أحمد علي");
        dto.PreferredLanguage.ShouldBe("en");
    }

    [Fact]
    public async Task Refresh_rotates_tokens_and_detects_reuse()
    {
        var auth = await _client.RegisterAsync();

        var first = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(auth.RefreshToken));
        first.StatusCode.ShouldBe(HttpStatusCode.OK);
        var rotated = (await first.Content.ReadFromJsonAsync<AuthResponse>(HttpExtensions.Json))!;
        rotated.RefreshToken.ShouldNotBe(auth.RefreshToken);

        // Reusing the old token is rejected and revokes the new one too
        (await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(auth.RefreshToken)))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(rotated.RefreshToken)))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_revokes_refresh_token()
    {
        var (client, auth) = await factory.CreateCustomerClientAsync();
        (await client.PostAsJsonAsync("/api/v1/auth/logout", new RefreshTokenRequest(auth.RefreshToken)))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(auth.RefreshToken)))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Change_password_requires_current_password()
    {
        var (client, auth) = await factory.CreateCustomerClientAsync();
        (await client.PostAsJsonAsync("/api/v1/auth/change-password", new ChangePasswordRequest("Wrong12345", "NewHoney123")))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        (await client.PostAsJsonAsync("/api/v1/auth/change-password", new ChangePasswordRequest(AuthHelper.DefaultPassword, "NewHoney123")))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await factory.CreateClient().LoginAsync(auth.User.Email, "NewHoney123");
    }

    [Fact]
    public async Task Forgot_and_reset_password_flow()
    {
        var email = AuthHelper.UniqueEmail();
        await _client.RegisterAsync(email);

        (await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest(email)))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        var mail = factory.Emails.Sent.Last(m => m.To == email);
        var token = WebUtility.HtmlDecode(Regex.Match(mail.Body, "<code>(.+?)</code>").Groups[1].Value);

        (await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(email, "bad-token", "Reset12345")))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await _client.PostAsJsonAsync("/api/v1/auth/reset-password", new ResetPasswordRequest(email, token, "Reset12345")))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
        await _client.LoginAsync(email, "Reset12345");
    }

    [Fact]
    public async Task Forgot_password_for_unknown_email_still_returns_204()
    {
        (await _client.PostAsJsonAsync("/api/v1/auth/forgot-password", new ForgotPasswordRequest("nobody@test.local")))
            .StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Admin_can_sign_in_with_admin_role()
    {
        var auth = await _client.LoginAsync(HoneyHouseApiFactory.AdminEmail, HoneyHouseApiFactory.AdminPassword);
        auth.User.Roles.ShouldContain("Admin");
    }
}
