using HoneyHouse.Application.Features.Addresses;
using HoneyHouse.Application.Features.Cart;
using HoneyHouse.Application.Features.Catalog;
using HoneyHouse.Application.Features.Orders;
using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace HoneyHouse.Admin.Tests.Infrastructure;

/// <summary>Creates data through the real application services (same code paths as the API).</summary>
public static class TestData
{
    public static string Sku() => "T-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
    public static string Unique(string prefix = "t") => $"{prefix}-{Guid.NewGuid():N}"[..20];

    public static async Task<AdminProductDto> CreateProductAsync(this AdminAppFactory f, decimal price = 100, int stock = 10,
        string nameAr = "عسل اختبار", string nameEn = "Test Honey", int lowStockThreshold = 5)
    {
        AdminProductDto? product = null;
        await f.WithScopeAsync(async sp =>
        {
            var result = await sp.GetRequiredService<IProductAdminService>().CreateAsync(new CreateProductRequest(
                1, nameAr, nameEn, null, null, Unique("p"), null, true, false,
                [new VariantUpsertRequest(Sku(), 250, "250 جرام", "250 g", price, null, stock, lowStockThreshold)]));
            result.IsSuccess.ShouldBeTrue();
            product = result.Value;
        });
        return product!;
    }

    /// <summary>Creates a customer and places a cash-on-delivery order for one variant.</summary>
    public static async Task<OrderPlacedDto> PlaceOrderAsync(this AdminAppFactory f, int variantId, int quantity = 1,
        PaymentMethod method = PaymentMethod.CashOnDelivery, string? coupon = null)
    {
        var email = await f.CreateUserAsync(Roles.Customer);
        OrderPlacedDto? placed = null;
        await f.WithScopeAsync(async sp =>
        {
            var user = await sp.GetRequiredService<UserManager<ApplicationUser>>().FindByEmailAsync(email);
            var address = await sp.GetRequiredService<IAddressService>().CreateAsync(user!.Id,
                new AddressUpsertRequest(1, "محمد أحمد", "01012345678", "مدينة نصر", null, "شارع عباس العقاد", "12", null, null, null, true));
            address.IsSuccess.ShouldBeTrue();
            (await sp.GetRequiredService<ICartService>().AddItemAsync(user.Id, new AddCartItemRequest(variantId, quantity))).IsSuccess.ShouldBeTrue();
            var order = await sp.GetRequiredService<IOrderService>().PlaceOrderAsync(user.Id,
                new PlaceOrderRequest(address.Value.Id, method, coupon, null));
            order.IsSuccess.ShouldBeTrue(string.Join(",", order.Errors.Select(e => e.Code)));
            placed = order.Value;
        });
        return placed!;
    }
}
