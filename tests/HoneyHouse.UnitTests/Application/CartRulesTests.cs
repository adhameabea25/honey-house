using HoneyHouse.Application.Features.Cart;
using HoneyHouse.Domain.Entities;
using Shouldly;

namespace HoneyHouse.UnitTests.Application;

public class CartRulesTests
{
    private static ProductVariant Variant(int stock = 10, bool active = true, bool productActive = true, bool categoryActive = true) => new()
    {
        StockQuantity = stock, IsActive = active,
        Product = new Product { IsActive = productActive, Category = new Category { IsActive = categoryActive } }
    };

    [Fact]
    public void Sellable_requires_active_variant_product_and_category()
    {
        CartService.IsSellable(Variant()).ShouldBeTrue();
        CartService.IsSellable(Variant(active: false)).ShouldBeFalse();
        CartService.IsSellable(Variant(productActive: false)).ShouldBeFalse();
        CartService.IsSellable(Variant(categoryActive: false)).ShouldBeFalse();
        CartService.IsSellable(new ProductVariant { IsActive = true, Product = null! }).ShouldBeFalse();
    }

    [Fact]
    public void Quantity_rules()
    {
        CartService.CheckQuantity(Variant(stock: 5), 5).IsSuccess.ShouldBeTrue();
        CartService.CheckQuantity(Variant(stock: 5), 6).FirstError!.Code.ShouldBe("insufficient_stock");
        CartService.CheckQuantity(Variant(stock: 100), 21).FirstError!.Code.ShouldBe("max_quantity_exceeded");
    }
}
