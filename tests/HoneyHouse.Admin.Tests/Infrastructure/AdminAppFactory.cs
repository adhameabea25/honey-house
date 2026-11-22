using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyHouse.Admin.Tests.Infrastructure;

/// <summary>Boots the real admin dashboard against a throw-away SQL Server database (server from HH_TEST_SQL).</summary>
public class AdminAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@test.local";
    public const string Password = "Admin12345";

    public string UploadsPath { get; } = Path.Combine(Path.GetTempPath(), $"hh-admin-uploads-{Guid.NewGuid():N}");
    private readonly string _databaseName = $"HoneyHouse_AdminTest_{Guid.NewGuid():N}";

    private string ConnectionString =>
        $"{Environment.GetEnvironmentVariable("HH_TEST_SQL") ?? "Server=localhost,1433;User Id=sa;Password=HoneyDev#2026;TrustServerCertificate=True"};Database={_databaseName}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = ConnectionString,
            ["Seed:ApplyMigrations"] = "true",
            ["Seed:SampleData"] = "true",
            ["Seed:AdminEmail"] = AdminEmail,
            ["Seed:AdminPassword"] = Password,
            ["FileStorage:RootPath"] = UploadsPath,
        }));
    }

    public Task InitializeAsync()
    {
        _ = Server;
        return Task.CompletedTask;
    }

    public HttpClient CreateBrowser() => CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    public async Task WithScopeAsync(Func<IServiceProvider, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider);
    }

    public async Task WithDbAsync(Func<AppDbContext, Task> action) =>
        await WithScopeAsync(sp => action(sp.GetRequiredService<AppDbContext>()));

    /// <summary>Creates a user with the given role and returns its email.</summary>
    public async Task<string> CreateUserAsync(string role, string? email = null)
    {
        email ??= $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@test.local";
        await WithScopeAsync(async sp =>
        {
            var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var user = new ApplicationUser { UserName = email, Email = email, FullName = $"Test {role}", EmailConfirmed = true };
            (await users.CreateAsync(user, Password)).Succeeded.ShouldBeTrue();
            await users.AddToRoleAsync(user, role);
        });
        return email;
    }

    public async Task UpdateUserAsync(string email, Func<UserManager<ApplicationUser>, ApplicationUser, Task> change) =>
        await WithScopeAsync(async sp =>
        {
            var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await users.FindByEmailAsync(email) ?? throw new InvalidOperationException(email);
            await change(users, user);
        });

    async Task IAsyncLifetime.DisposeAsync()
    {
        await WithDbAsync(db => db.Database.EnsureDeletedAsync());
        if (Directory.Exists(UploadsPath)) Directory.Delete(UploadsPath, recursive: true);
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class AdminCollection : ICollectionFixture<AdminAppFactory>
{
    public const string Name = "admin";
}
