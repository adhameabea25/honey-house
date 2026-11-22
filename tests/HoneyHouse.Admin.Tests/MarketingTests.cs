using System.Net;
using HoneyHouse.Admin.Tests.Infrastructure;
using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyHouse.Admin.Tests;

[Collection(AdminCollection.Name)]
public class MarketingTests(AdminAppFactory factory)
{
    [Fact]
    public async Task Coupon_create_edit_and_use_in_order()
    {
        var client = await factory.SignedInAsync();
        var code = ("T" + Guid.NewGuid().ToString("N")[..8]).ToUpperInvariant();

        var created = await client.SubmitAsync("/Coupons/Create", "/Coupons/Create", new Dictionary<string, string>
        {
            ["Code"] = code.ToLowerInvariant(), ["DescriptionAr"] = "خصم الاختبار", ["DiscountType"] = "Percentage", ["Value"] = "10",
            ["MaxDiscountAmount"] = "50", ["StartsAt"] = "2026-01-01T00:00", ["ExpiresAt"] = "2030-12-31T23:59", ["IsActive"] = "true"
        });
        created.StatusCode.ShouldBe(HttpStatusCode.Redirect, await created.Content.ReadAsStringAsync());

        Coupon? coupon = null;
        await factory.WithDbAsync(async db => coupon = await db.Coupons.AsNoTracking().SingleAsync(c => c.Code == code));
        coupon!.StartsAt.ShouldBe(new DateTime(2025, 12, 31, 22, 0, 0)); // Cairo midnight in UTC (winter, UTC+2)

        var list = await client.GetPageAsync("/Coupons?search=" + code);
        list.ShouldContain(code);
        list.ShouldContain("10%");

        var edit = await client.SubmitAsync($"/Coupons/Edit/{coupon.Id}", $"/Coupons/Edit/{coupon.Id}", new Dictionary<string, string>
        {
            ["Code"] = code, ["DiscountType"] = "FixedAmount", ["Value"] = "25", ["IsActive"] = "true"
        });
        edit.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var product = await factory.CreateProductAsync(price: 200);
        var placed = await factory.PlaceOrderAsync(product.Variants[0].Id, coupon: code);
        placed.Order.DiscountAmount.ShouldBe(25);

        // Used coupons cannot be deleted
        var del = await client.SubmitAsync("/Coupons", $"/Coupons/Delete/{coupon.Id}", new { });
        del.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await client.GetPageAsync("/Coupons")).ShouldContain("لا يمكن حذف كود خصم تم استخدامه");
    }

    [Fact]
    public async Task Coupon_validation_errors_are_shown()
    {
        var client = await factory.SignedInAsync();
        var r = await client.SubmitAsync("/Coupons/Create", "/Coupons/Create", new Dictionary<string, string>
        {
            ["Code"] = "bad code!", ["DiscountType"] = "Percentage", ["Value"] = "150",
            ["StartsAt"] = "2026-05-01T00:00", ["ExpiresAt"] = "2026-04-01T00:00"
        });
        r.StatusCode.ShouldBe(HttpStatusCode.OK);
        var html = await r.Content.ReadAsStringAsync();
        html.ShouldContain("الصيغة غير صحيحة");
        html.ShouldContain("يجب أن تكون القيمة 100 أو أقل");
    }

    [Fact]
    public async Task Duplicate_coupon_code_is_refused()
    {
        var client = await factory.SignedInAsync();
        var code = ("D" + Guid.NewGuid().ToString("N")[..8]).ToUpperInvariant();
        var fields = new Dictionary<string, string> { ["Code"] = code, ["DiscountType"] = "FixedAmount", ["Value"] = "5", ["IsActive"] = "true" };
        (await client.SubmitAsync("/Coupons/Create", "/Coupons/Create", fields)).StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var again = await client.SubmitAsync("/Coupons/Create", "/Coupons/Create", fields);
        (await again.Content.ReadAsStringAsync()).ShouldContain("كود الخصم موجود بالفعل");
    }

    [Fact]
    public async Task Unused_coupon_can_be_deleted()
    {
        var client = await factory.SignedInAsync();
        var code = ("X" + Guid.NewGuid().ToString("N")[..8]).ToUpperInvariant();
        await client.SubmitAsync("/Coupons/Create", "/Coupons/Create", new Dictionary<string, string> { ["Code"] = code, ["DiscountType"] = "FixedAmount", ["Value"] = "5", ["IsActive"] = "true" });
        int id = 0;
        await factory.WithDbAsync(async db => id = (await db.Coupons.SingleAsync(c => c.Code == code)).Id);
        (await client.SubmitAsync("/Coupons", $"/Coupons/Delete/{id}", new { })).StatusCode.ShouldBe(HttpStatusCode.Redirect);
        await factory.WithDbAsync(async db => (await db.Coupons.AnyAsync(c => c.Code == code)).ShouldBeFalse());
    }

    [Fact]
    public async Task Review_moderation_publishes_hides_and_deletes()
    {
        var product = await factory.CreateProductAsync(nameAr: "عسل للتقييم");
        var email = await factory.CreateUserAsync(Roles.Customer);
        int reviewId = 0;
        await factory.WithScopeAsync(async sp =>
        {
            var user = await sp.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email);
            var r = await sp.GetRequiredService<HoneyHouse.Application.Features.Reviews.IReviewService>()
                .CreateAsync(user!.Id, product.Id, new(4, "طعم رائع ومميز"));
            r.IsSuccess.ShouldBeTrue();
            reviewId = r.Value.Id;
        });

        var client = await factory.SignedInAsync(await factory.CreateUserAsync(Roles.Manager));
        var pending = await client.GetPageAsync("/Reviews");
        pending.ShouldContain("طعم رائع ومميز");

        (await client.SubmitAsync("/Reviews", $"/Reviews/Moderate/{reviewId}", new Dictionary<string, string> { ["approved"] = "true", ["returnUrl"] = "/Reviews" }))
            .Location().ShouldBe("/Reviews");
        await factory.WithDbAsync(async db =>
        {
            (await db.Reviews.SingleAsync(r => r.Id == reviewId)).IsApproved.ShouldBeTrue();
            (await db.Products.SingleAsync(p => p.Id == product.Id)).ReviewCount.ShouldBe(1);
        });
        (await client.GetPageAsync("/Reviews?status=approved")).ShouldContain("طعم رائع ومميز");

        (await client.SubmitAsync("/Reviews", $"/Reviews/Moderate/{reviewId}", new Dictionary<string, string> { ["approved"] = "false" })).StatusCode.ShouldBe(HttpStatusCode.Redirect);
        await factory.WithDbAsync(async db => (await db.Products.SingleAsync(p => p.Id == product.Id)).ReviewCount.ShouldBe(0));

        (await client.SubmitAsync("/Reviews", $"/Reviews/Delete/{reviewId}", new { })).StatusCode.ShouldBe(HttpStatusCode.Redirect);
        await factory.WithDbAsync(async db => (await db.Reviews.AnyAsync(r => r.Id == reviewId)).ShouldBeFalse());
    }

    [Fact]
    public async Task Shipping_fee_update_applies_to_new_orders()
    {
        var client = await factory.SignedInAsync();
        var page = await client.GetPageAsync("/Governorates");
        page.ShouldContain("القاهرة");

        // Governorate 1 is used by TestData orders
        var r = await client.PostFormAsync("/Governorates/Update/1",
            [new("ShippingFee", "65.5"), new("IsActive", "true"), new("IsActive", "false")], BrowserExtensions.Token(page));
        r.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        (await client.GetPageAsync("/Governorates")).ShouldContain("value=\"65.5\"");

        var product = await factory.CreateProductAsync(price: 100);
        var placed = await factory.PlaceOrderAsync(product.Variants[0].Id);
        placed.Order.ShippingFee.ShouldBe(65.5m);
    }

    [Fact]
    public async Task Negative_shipping_fee_is_rejected()
    {
        var client = await factory.SignedInAsync();
        var page = await client.GetPageAsync("/Governorates");
        await client.PostFormAsync("/Governorates/Update/2", [new("ShippingFee", "-5"), new("IsActive", "false")], BrowserExtensions.Token(page));
        (await client.GetPageAsync("/Governorates")).ShouldContain("يجب أن تكون القيمة 0 أو أكثر");
    }

    [Theory]
    [InlineData("/Coupons")]
    [InlineData("/Governorates")]
    public async Task Admin_only_pages_are_denied_to_managers(string url)
    {
        var client = await factory.SignedInAsync(await factory.CreateUserAsync(Roles.Manager));
        var r = await client.GetAsync(url);
        r.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        r.Location().ShouldContain("/Account/AccessDenied");
    }
}
