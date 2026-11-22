using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HoneyHouse.Infrastructure.Persistence.Seed;

public class DatabaseSeeder(
    AppDbContext db,
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    IOptions<SeedOptions> options,
    ILogger<DatabaseSeeder> logger)
{
    private readonly SeedOptions _options = options.Value;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        if (_options.ApplyMigrations && db.Database.IsRelational())
            await db.Database.MigrateAsync(ct);

        await SeedRolesAsync();
        await SeedAdminAsync();
        await SeedGovernoratesAsync(ct);
        if (_options.SampleData)
            await SampleCatalog.SeedAsync(db, ct);
    }

    private async Task SeedRolesAsync()
    {
        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
    }

    private async Task SeedAdminAsync()
    {
        if (string.IsNullOrWhiteSpace(_options.AdminEmail) || string.IsNullOrWhiteSpace(_options.AdminPassword))
            return;
        if (await userManager.FindByEmailAsync(_options.AdminEmail) is not null)
            return;

        var admin = new ApplicationUser
        {
            UserName = _options.AdminEmail,
            Email = _options.AdminEmail,
            EmailConfirmed = true,
            FullName = _options.AdminFullName
        };
        var result = await userManager.CreateAsync(admin, _options.AdminPassword);
        if (!result.Succeeded)
        {
            logger.LogError("Failed to seed admin user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
            return;
        }
        await userManager.AddToRoleAsync(admin, Roles.Admin);
        logger.LogInformation("Seeded admin user {Email}", _options.AdminEmail);
    }

    private async Task SeedGovernoratesAsync(CancellationToken ct)
    {
        if (await db.Governorates.AnyAsync(ct)) return;
        db.Governorates.AddRange(EgyptGovernorates.All());
        await db.SaveChangesAsync(ct);
    }
}
