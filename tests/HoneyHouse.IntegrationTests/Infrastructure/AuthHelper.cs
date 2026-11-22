using System.Net.Http.Headers;
using System.Net.Http.Json;
using HoneyHouse.Application.Features.Auth;

namespace HoneyHouse.IntegrationTests.Infrastructure;

public static class AuthHelper
{
    public const string DefaultPassword = "Honey12345";

    public static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@test.local";

    public static async Task<AuthResponse> RegisterAsync(this HttpClient client, string? email = null, string password = DefaultPassword)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/register",
            new RegisterRequest("Test Customer", email ?? UniqueEmail(), "01012345678", password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(HttpExtensions.Json))!;
    }

    public static async Task<AuthResponse> LoginAsync(this HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(email, password));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<AuthResponse>(HttpExtensions.Json))!;
    }

    public static HttpClient Authorize(this HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    /// <summary>A client signed in as a brand new customer.</summary>
    public static async Task<(HttpClient Client, AuthResponse Auth)> CreateCustomerClientAsync(this HoneyHouseApiFactory factory)
    {
        var client = factory.CreateClient();
        var auth = await client.RegisterAsync();
        return (client.Authorize(auth.AccessToken), auth);
    }

    /// <summary>A client signed in as the seeded admin.</summary>
    public static async Task<HttpClient> CreateAdminClientAsync(this HoneyHouseApiFactory factory)
    {
        var client = factory.CreateClient();
        var auth = await client.LoginAsync(HoneyHouseApiFactory.AdminEmail, HoneyHouseApiFactory.AdminPassword);
        return client.Authorize(auth.AccessToken);
    }
}
