using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Infrastructure.Persistence.Seed;
using HoneyHouse.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace HoneyHouse.IntegrationTests;

[Collection(ApiCollection.Name)]
public class DatabaseTests(HoneyHouseApiFactory factory)
{
    [Fact]
    public Task Migrations_are_fully_applied() => factory.WithDbAsync(async db =>
    {
        (await db.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        (await db.Database.GetAppliedMigrationsAsync()).ShouldNotBeEmpty();
    });

    [Fact]
    public Task Seeds_all_27_governorates() => factory.WithDbAsync(async db =>
    {
        (await db.Governorates.CountAsync()).ShouldBe(27);
        (await db.Governorates.SingleAsync(g => g.NameEn == "Cairo")).NameAr.ShouldBe("القاهرة");
    });

    [Fact]
    public async Task Seeds_roles_and_admin_user()
    {
        using var scope = factory.Services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var role in Roles.All) (await roles.RoleExistsAsync(role)).ShouldBeTrue();
        var admin = await users.FindByEmailAsync(HoneyHouseApiFactory.AdminEmail);
        admin.ShouldNotBeNull();
        (await users.IsInRoleAsync(admin, Roles.Admin)).ShouldBeTrue();
    }

    [Fact]
    public Task Sample_catalog_has_products_with_weight_variants() => factory.WithDbAsync(async db =>
    {
        var sidr = await db.Products.Include(p => p.Variants).SingleAsync(p => p.Slug == "sidr-honey");
        sidr.Variants.Select(v => v.WeightGrams).OrderBy(w => w).ShouldBe([250, 500, 1000]);
        sidr.Variants.ShouldAllBe(v => v.Price > 0);
    });

    [Fact]
    public async Task Seeding_twice_is_idempotent()
    {
        using var scope = factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
        await factory.WithDbAsync(async db =>
        {
            (await db.Governorates.CountAsync()).ShouldBe(27);
            (await db.Categories.CountAsync()).ShouldBe(3);
        });
    }

    [Fact]
    public Task Stock_cannot_go_negative() => factory.WithDbAsync(async db =>
    {
        var variant = await db.ProductVariants.FirstAsync();
        variant.StockQuantity = -1;
        await Should.ThrowAsync<DbUpdateException>(() => db.SaveChangesAsync());
    });

    [Fact]
    public async Task Concurrent_stock_updates_are_detected()
    {
        int variantId = 0;
        await factory.WithDbAsync(async db => variantId = (await db.ProductVariants.FirstAsync()).Id);

        using var s1 = factory.Services.CreateScope();
        using var s2 = factory.Services.CreateScope();
        var db1 = s1.ServiceProvider.GetRequiredService<HoneyHouse.Infrastructure.Persistence.AppDbContext>();
        var db2 = s2.ServiceProvider.GetRequiredService<HoneyHouse.Infrastructure.Persistence.AppDbContext>();
        var v1 = await db1.ProductVariants.SingleAsync(v => v.Id == variantId);
        var v2 = await db2.ProductVariants.SingleAsync(v => v.Id == variantId);

        v1.StockQuantity -= 1;
        await db1.SaveChangesAsync();
        v2.StockQuantity -= 1;
        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => db2.SaveChangesAsync());
    }
}
