using System.Net;
using System.Net.Http.Json;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Cart;
using HoneyHouse.Application.Features.Coupons;
using HoneyHouse.Domain.Enums;
using HoneyHouse.IntegrationTests.Infrastructure;
using Shouldly;

namespace HoneyHouse.IntegrationTests;

[Collection(ApiCollection.Name)]
public class CouponTests(HoneyHouseApiFactory factory)
{
    private static string Code() => "C" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();

    private static CouponUpsertRequest Coupon(string code, DiscountType type = DiscountType.Percentage, decimal value = 10,
        decimal? min = null, decimal? max = null, DateTime? starts = null, DateTime? expires = null, int? limit = null, int? perUser = null) =>
        new(code, "خصم", "Discount", type, value, min, max, starts, expires, limit, perUser);

    private async Task<HttpClient> CustomerWithCartAsync(HttpClient admin, decimal price = 100, int qty = 2)
    {
        var product = await admin.CreateProductAsync(price: price);
        var (client, _) = await factory.CreateCustomerClientAsync();
        (await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(product.Variants[0].Id, qty))).EnsureSuccessStatusCode();
        return client;
    }

    [Fact]
    public async Task Admin_coupon_crud()
    {
        var admin = await factory.CreateAdminClientAsync();
        var code = Code();
        var res = await admin.PostAsJsonAsync("/api/v1/admin/coupons", Coupon(code.ToLowerInvariant()));
        res.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = (await res.Content.ReadFromJsonAsync<AdminCouponDto>(HttpExtensions.Json))!;
        created.Code.ShouldBe(code); // normalized to upper case

        (await admin.PostAsJsonAsync("/api/v1/admin/coupons", Coupon(code))).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        var upd = await admin.PutAsJsonAsync($"/api/v1/admin/coupons/{created.Id}", Coupon(code, DiscountType.FixedAmount, 30) with { IsActive = false });
        var dto = (await upd.Content.ReadFromJsonAsync<AdminCouponDto>(HttpExtensions.Json))!;
        dto.DiscountType.ShouldBe(DiscountType.FixedAmount);
        dto.IsActive.ShouldBeFalse();

        var list = await admin.GetFromJsonAsync<PagedResult<AdminCouponDto>>($"/api/v1/admin/coupons?search={code}", HttpExtensions.Json);
        list!.Items.ShouldHaveSingleItem();

        (await admin.DeleteAsync($"/api/v1/admin/coupons/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await admin.GetAsync($"/api/v1/admin/coupons/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Coupon_validation_rules_on_create()
    {
        var admin = await factory.CreateAdminClientAsync();
        (await admin.PostAsJsonAsync("/api/v1/admin/coupons", Coupon(Code(), value: 150))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await admin.PostAsJsonAsync("/api/v1/admin/coupons", Coupon("bad code!"))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await admin.PostAsJsonAsync("/api/v1/admin/coupons",
            Coupon(Code(), starts: DateTime.UtcNow, expires: DateTime.UtcNow.AddDays(-1)))).StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Managers_and_customers_cannot_manage_coupons()
    {
        var (customer, _) = await factory.CreateCustomerClientAsync();
        (await customer.GetAsync("/api/v1/admin/coupons")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Validate_percentage_coupon_against_cart_with_cap()
    {
        var admin = await factory.CreateAdminClientAsync();
        var code = Code();
        await admin.PostAsJsonAsync("/api/v1/admin/coupons", Coupon(code, value: 20, max: 30));
        var client = await CustomerWithCartAsync(admin, price: 100, qty: 2); // subtotal 200 => 20% = 40, capped to 30

        var res = await client.PostAsJsonAsync("/api/v1/coupons/validate", new ValidateCouponRequest(code.ToLowerInvariant()));
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = (await res.Content.ReadFromJsonAsync<CouponValidationDto>(HttpExtensions.Json))!;
        dto.Subtotal.ShouldBe(200);
        dto.DiscountAmount.ShouldBe(30);
        dto.SubtotalAfterDiscount.ShouldBe(170);
    }

    [Theory]
    [InlineData("expired", "coupon_expired")]
    [InlineData("future", "coupon_not_started")]
    [InlineData("min", "coupon_min_order")]
    [InlineData("inactive", "coupon_invalid")]
    [InlineData("unknown", "coupon_invalid")]
    public async Task Ineligible_coupons_are_rejected(string scenario, string expectedCode)
    {
        var admin = await factory.CreateAdminClientAsync();
        var code = Code();
        var request = scenario switch
        {
            "expired" => Coupon(code, expires: DateTime.UtcNow.AddMinutes(-1)),
            "future" => Coupon(code, starts: DateTime.UtcNow.AddDays(1)),
            "min" => Coupon(code, min: 1000),
            "inactive" => Coupon(code) with { IsActive = false },
            _ => null
        };
        if (request is not null) (await admin.PostAsJsonAsync("/api/v1/admin/coupons", request)).EnsureSuccessStatusCode();

        var client = await CustomerWithCartAsync(admin);
        var res = await client.PostAsJsonAsync("/api/v1/coupons/validate", new ValidateCouponRequest(code));
        res.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await res.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe(expectedCode);
    }

    [Fact]
    public async Task Validate_with_empty_cart_fails()
    {
        var admin = await factory.CreateAdminClientAsync();
        var code = Code();
        await admin.PostAsJsonAsync("/api/v1/admin/coupons", Coupon(code));
        var (client, _) = await factory.CreateCustomerClientAsync();
        var res = await client.PostAsJsonAsync("/api/v1/coupons/validate", new ValidateCouponRequest(code));
        (await res.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("cart_empty");
    }
}
