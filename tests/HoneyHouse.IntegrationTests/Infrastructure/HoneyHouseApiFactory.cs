using HoneyHouse.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyHouse.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API against a throw-away SQL Server database.
/// Server is taken from HH_TEST_SQL (defaults to the docker-compose SQL Server on localhost).
/// </summary>
public class HoneyHouseApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin12345";

    private readonly string _databaseName = $"HoneyHouse_Test_{Guid.NewGuid():N}";

    public string ConnectionString
    {
        get
        {
            var server = Environment.GetEnvironmentVariable("HH_TEST_SQL")
                         ?? "Server=localhost,1433;User Id=sa;Password=HoneyDev#2026;TrustServerCertificate=True";
            return $"{server};Database={_databaseName}";
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = ConnectionString,
                ["Seed:ApplyMigrations"] = "true",
                ["Seed:SampleData"] = "true",
                ["Seed:AdminEmail"] = AdminEmail,
                ["Seed:AdminPassword"] = AdminPassword
            });
        });
    }

    public Task InitializeAsync()
    {
        _ = Server; // forces host start => migrations + seed
        return Task.CompletedTask;
    }

    public async Task WithDbAsync(Func<AppDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<AppDbContext>());
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await WithDbAsync(db => db.Database.EnsureDeletedAsync());
        await base.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<HoneyHouseApiFactory>
{
    public const string Name = "api";
}
