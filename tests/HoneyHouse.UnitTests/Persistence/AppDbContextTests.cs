using HoneyHouse.Domain.Entities;
using HoneyHouse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace HoneyHouse.UnitTests.Persistence;

public class AppDbContextTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 22, 10, 0, 0, TimeSpan.Zero);

    private static AppDbContext CreateDb(TimeProvider clock) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options, clock);

    [Fact]
    public async Task Sets_created_and_updated_timestamps()
    {
        var clock = new FakeTimeProvider(Now);
        await using var db = CreateDb(clock);
        var gov = new Governorate { NameAr = "القاهرة", NameEn = "Cairo", ShippingFee = 50 };
        db.Governorates.Add(gov);
        await db.SaveChangesAsync();
        gov.CreatedAt.ShouldBe(Now.UtcDateTime);
        gov.UpdatedAt.ShouldBeNull();

        clock.Advance(TimeSpan.FromHours(1));
        gov.ShippingFee = 60;
        await db.SaveChangesAsync();
        gov.UpdatedAt.ShouldBe(Now.AddHours(1).UtcDateTime);
    }

    [Fact]
    public async Task Deleting_a_category_soft_deletes_and_hides_it()
    {
        await using var db = CreateDb(new FakeTimeProvider(Now));
        var cat = new Category { NameAr = "عسل", NameEn = "Honey", Slug = "honey" };
        db.Categories.Add(cat);
        await db.SaveChangesAsync();

        db.Categories.Remove(cat);
        await db.SaveChangesAsync();

        (await db.Categories.CountAsync()).ShouldBe(0);
        var raw = await db.Categories.IgnoreQueryFilters().SingleAsync();
        raw.IsDeleted.ShouldBeTrue();
        raw.DeletedAt.ShouldBe(Now.UtcDateTime);
    }
}
