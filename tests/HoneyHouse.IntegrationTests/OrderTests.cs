using System.Net;
using System.Net.Http.Json;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Cart;
using HoneyHouse.Application.Features.Catalog;
using HoneyHouse.Application.Features.Coupons;
using HoneyHouse.Application.Features.Inventory;
using HoneyHouse.Application.Features.Orders;
using HoneyHouse.Domain.Enums;
using HoneyHouse.IntegrationTests.Infrastructure;
using Shouldly;

namespace HoneyHouse.IntegrationTests;

[Collection(ApiCollection.Name)]
public class OrderTests(HoneyHouseApiFactory factory)
{
    private sealed record Shopper(HttpClient Client, int AddressId);

    private async Task<Shopper> ShopperWithCartAsync(params (int VariantId, int Qty)[] items)
    {
        var (client, _) = await factory.CreateCustomerClientAsync();
        var address = await client.CreateAddressAsync(governorateId: 1); // Cairo, 50 EGP
        foreach (var (variantId, qty) in items)
            (await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(variantId, qty))).EnsureSuccessStatusCode();
        return new Shopper(client, address.Id);
    }

    private static async Task<OrderPlacedDto> PlaceAsync(Shopper s, string? coupon = null)
    {
        var res = await s.Client.PostAsJsonAsync("/api/v1/orders", new PlaceOrderRequest(s.AddressId, PaymentMethod.CashOnDelivery, coupon, "اتصل قبل الوصول"));
        res.StatusCode.ShouldBe(HttpStatusCode.Created, await res.Content.ReadAsStringAsync());
        return (await res.Content.ReadFromJsonAsync<OrderPlacedDto>(HttpExtensions.Json))!;
    }

    private static async Task<int> StockAsync(HttpClient admin, int productId, int variantId) =>
        (await admin.GetFromJsonAsync<AdminProductDto>($"/api/v1/admin/products/{productId}", HttpExtensions.Json))!
        .Variants.Single(v => v.Id == variantId).StockQuantity;

    private static async Task<string> CreateCouponAsync(HttpClient admin, decimal percent = 10)
    {
        var code = "O" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
        (await admin.PostAsJsonAsync("/api/v1/admin/coupons",
            new CouponUpsertRequest(code, null, null, DiscountType.Percentage, percent, null, null, null, null, null, 1))).EnsureSuccessStatusCode();
        return code;
    }

    private static Task<HttpResponseMessage> SetStatus(HttpClient admin, int orderId, OrderStatus status, string? tracking = null) =>
        admin.PutAsJsonAsync($"/api/v1/admin/orders/{orderId}/status", new UpdateOrderStatusRequest(status, null, tracking));

    [Fact]
    public async Task Preview_shows_subtotal_discount_shipping_and_total()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(price: 100, stock: 10);
        var code = await CreateCouponAsync(admin, 10);
        var shopper = await ShopperWithCartAsync((product.Variants[0].Id, 3));

        var res = await shopper.Client.PostAsJsonAsync("/api/v1/orders/checkout/preview", new CheckoutPreviewRequest(shopper.AddressId, code));
        var preview = (await res.Content.ReadFromJsonAsync<CheckoutSummaryDto>(HttpExtensions.Json))!;
        preview.Subtotal.ShouldBe(300);
        preview.DiscountAmount.ShouldBe(30);
        preview.ShippingFee.ShouldBe(50);
        preview.Total.ShouldBe(320);
        preview.CanPlaceOrder.ShouldBeTrue();

        var bad = await shopper.Client.PostAsJsonAsync("/api/v1/orders/checkout/preview", new CheckoutPreviewRequest(shopper.AddressId, "NOPE"));
        var badPreview = (await bad.Content.ReadFromJsonAsync<CheckoutSummaryDto>(HttpExtensions.Json))!;
        badPreview.CouponErrorCode.ShouldBe("coupon_invalid");
        badPreview.DiscountAmount.ShouldBe(0);
        badPreview.Total.ShouldBe(350);
    }

    [Fact]
    public async Task Placing_cod_order_reserves_stock_clears_cart_and_uses_coupon()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(price: 100, stock: 10, stock1Kg: 5);
        var v250 = product.Variants.Single(v => v.WeightGrams == 250);
        var v1k = product.Variants.Single(v => v.WeightGrams == 1000);
        var code = await CreateCouponAsync(admin, 10);
        var shopper = await ShopperWithCartAsync((v250.Id, 2), (v1k.Id, 1));

        var placed = await PlaceAsync(shopper, code);
        var order = placed.Order;
        placed.PaymentUrl.ShouldBeNull();
        order.OrderNumber.ShouldStartWith("HH-");
        order.Status.ShouldBe(OrderStatus.Pending);
        order.PaymentStatus.ShouldBe(PaymentStatus.Pending);
        order.Subtotal.ShouldBe(200 + 300);
        order.DiscountAmount.ShouldBe(50);
        order.ShippingFee.ShouldBe(50);
        order.Total.ShouldBe(500);
        order.Items.Count.ShouldBe(2);
        order.ShippingAddress.Governorate.ShouldBe("القاهرة");
        order.CanCancel.ShouldBeTrue();

        (await StockAsync(admin, product.Id, v250.Id)).ShouldBe(8);
        (await StockAsync(admin, product.Id, v1k.Id)).ShouldBe(4);
        (await shopper.Client.GetFromJsonAsync<CartDto>("/api/v1/cart", HttpExtensions.Json))!.Items.ShouldBeEmpty();

        var tx = await admin.GetFromJsonAsync<PagedResult<InventoryTransactionDto>>($"/api/v1/admin/inventory/{v250.Id}/transactions", HttpExtensions.Json);
        tx!.Items.ShouldContain(t => t.Type == InventoryTransactionType.Sale && t.QuantityChange == -2 && t.OrderId == order.Id);

        var coupons = await admin.GetFromJsonAsync<PagedResult<AdminCouponDto>>($"/api/v1/admin/coupons?search={code}", HttpExtensions.Json);
        coupons!.Items.Single().UsedCount.ShouldBe(1);

        var mine = await shopper.Client.GetFromJsonAsync<PagedResult<OrderSummaryDto>>("/api/v1/orders", HttpExtensions.Json);
        mine!.Items.ShouldHaveSingleItem().Id.ShouldBe(order.Id);
    }

    [Fact]
    public async Task Empty_cart_cannot_be_ordered()
    {
        var shopper = await ShopperWithCartAsync();
        var res = await shopper.Client.PostAsJsonAsync("/api/v1/orders", new PlaceOrderRequest(shopper.AddressId, PaymentMethod.CashOnDelivery, null, null));
        res.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await res.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("cart_empty");
    }

    [Fact]
    public async Task Order_is_rejected_when_stock_dropped_after_adding_to_cart()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(stock: 5);
        var variant = product.Variants[0];
        var shopper = await ShopperWithCartAsync((variant.Id, 4));

        (await admin.PostAsJsonAsync($"/api/v1/admin/inventory/{variant.Id}/adjust",
            new AdjustStockRequest(-3, InventoryTransactionType.Adjustment, "Damaged jars"))).EnsureSuccessStatusCode();

        var res = await shopper.Client.PostAsJsonAsync("/api/v1/orders", new PlaceOrderRequest(shopper.AddressId, PaymentMethod.CashOnDelivery, null, null));
        res.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await res.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("order_has_unavailable_items");
        (await StockAsync(admin, product.Id, variant.Id)).ShouldBe(2);
    }

    [Fact]
    public async Task Two_customers_racing_for_the_last_unit_cannot_oversell()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(stock: 1);
        var variant = product.Variants[0];
        var a = await ShopperWithCartAsync((variant.Id, 1));
        var b = await ShopperWithCartAsync((variant.Id, 1));

        var responses = await Task.WhenAll(
            a.Client.PostAsJsonAsync("/api/v1/orders", new PlaceOrderRequest(a.AddressId, PaymentMethod.CashOnDelivery, null, null)),
            b.Client.PostAsJsonAsync("/api/v1/orders", new PlaceOrderRequest(b.AddressId, PaymentMethod.CashOnDelivery, null, null)));

        responses.Count(r => r.StatusCode == HttpStatusCode.Created).ShouldBe(1);
        responses.Single(r => r.StatusCode != HttpStatusCode.Created).StatusCode
            .ShouldBeOneOf(HttpStatusCode.Conflict, HttpStatusCode.UnprocessableEntity);
        (await StockAsync(admin, product.Id, variant.Id)).ShouldBe(0);
    }

    [Fact]
    public async Task Customer_cancel_restores_stock_and_releases_coupon()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(stock: 10);
        var variant = product.Variants[0];
        var code = await CreateCouponAsync(admin);
        var shopper = await ShopperWithCartAsync((variant.Id, 3));
        var order = (await PlaceAsync(shopper, code)).Order;
        (await StockAsync(admin, product.Id, variant.Id)).ShouldBe(7);

        var res = await shopper.Client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/cancel", new CancelOrderRequest("غيرت رأيي"));
        res.StatusCode.ShouldBe(HttpStatusCode.OK);
        var cancelled = (await res.Content.ReadFromJsonAsync<OrderDetailDto>(HttpExtensions.Json))!;
        cancelled.Status.ShouldBe(OrderStatus.Cancelled);
        cancelled.CancelledAt.ShouldNotBeNull();

        (await StockAsync(admin, product.Id, variant.Id)).ShouldBe(10);
        var coupons = await admin.GetFromJsonAsync<PagedResult<AdminCouponDto>>($"/api/v1/admin/coupons?search={code}", HttpExtensions.Json);
        coupons!.Items.Single().UsedCount.ShouldBe(0);

        // Per-user limit of 1 is free again
        (await shopper.Client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(variant.Id, 1))).EnsureSuccessStatusCode();
        await PlaceAsync(shopper, code);
    }

    [Fact]
    public async Task Admin_moves_order_through_lifecycle_and_cod_is_paid_on_delivery()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(stock: 10);
        var shopper = await ShopperWithCartAsync((product.Variants[0].Id, 1));
        var order = (await PlaceAsync(shopper)).Order;

        (await SetStatus(admin, order.Id, OrderStatus.Delivered)).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);

        foreach (var s in new[] { OrderStatus.Confirmed, OrderStatus.Processing })
            (await SetStatus(admin, order.Id, s)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await SetStatus(admin, order.Id, OrderStatus.Shipped, "ARAMEX-123")).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Customer can no longer cancel
        var cancel = await shopper.Client.PostAsJsonAsync($"/api/v1/orders/{order.Id}/cancel", new CancelOrderRequest(null));
        (await cancel.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("order_cannot_cancel");

        var delivered = await SetStatus(admin, order.Id, OrderStatus.Delivered);
        var detail = (await delivered.Content.ReadFromJsonAsync<AdminOrderDetailDto>(HttpExtensions.Json))!;
        detail.Order.Status.ShouldBe(OrderStatus.Delivered);
        detail.Order.PaymentStatus.ShouldBe(PaymentStatus.Paid);
        detail.Order.TrackingNumber.ShouldBe("ARAMEX-123");
        detail.Payments.ShouldHaveSingleItem().Status.ShouldBe(PaymentStatus.Paid);
        detail.AllowedNextStatuses.ShouldBe([OrderStatus.Returned]);

        var tracking = await shopper.Client.WithLanguage("ar")
            .GetFromJsonAsync<List<OrderStatusEntryDto>>($"/api/v1/orders/{order.Id}/tracking", HttpExtensions.Json);
        tracking!.Select(t => t.Status).ShouldBe([OrderStatus.Pending, OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Delivered]);
        tracking!.Last().StatusName.ShouldBe("تم التوصيل");
    }

    [Fact]
    public async Task Returned_order_restocks()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(stock: 10);
        var variant = product.Variants[0];
        var shopper = await ShopperWithCartAsync((variant.Id, 2));
        var order = (await PlaceAsync(shopper)).Order;
        foreach (var s in new[] { OrderStatus.Confirmed, OrderStatus.Processing, OrderStatus.Shipped, OrderStatus.Returned })
            (await SetStatus(admin, order.Id, s)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await StockAsync(admin, product.Id, variant.Id)).ShouldBe(10);
    }

    [Fact]
    public async Task Admin_cancel_and_mark_paid_rules()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(stock: 10);
        var shopper = await ShopperWithCartAsync((product.Variants[0].Id, 1));
        var order = (await PlaceAsync(shopper)).Order;

        (await admin.PostAsync($"/api/v1/admin/orders/{order.Id}/mark-paid", null)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await admin.PostAsync($"/api/v1/admin/orders/{order.Id}/mark-paid", null)).StatusCode.ShouldBe(HttpStatusCode.Conflict);

        (await SetStatus(admin, order.Id, OrderStatus.Cancelled)).StatusCode.ShouldBe(HttpStatusCode.OK);
        var detail = await admin.GetFromJsonAsync<AdminOrderDetailDto>($"/api/v1/admin/orders/{order.Id}", HttpExtensions.Json);
        detail!.Order.History.Last().Note!.ShouldContain("refund required");
        (await StockAsync(admin, product.Id, product.Variants[0].Id)).ShouldBe(10);
    }

    [Fact]
    public async Task Customers_only_see_their_own_orders()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(stock: 10);
        var owner = await ShopperWithCartAsync((product.Variants[0].Id, 1));
        var order = (await PlaceAsync(owner)).Order;

        var (other, _) = await factory.CreateCustomerClientAsync();
        (await other.GetAsync($"/api/v1/orders/{order.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await other.PostAsJsonAsync($"/api/v1/orders/{order.Id}/cancel", new CancelOrderRequest(null))).StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await owner.Client.GetAsync("/api/v1/admin/orders")).StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_order_search_filters()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(stock: 10);
        var shopper = await ShopperWithCartAsync((product.Variants[0].Id, 1));
        var order = (await PlaceAsync(shopper)).Order;

        var byNumber = await admin.GetFromJsonAsync<PagedResult<AdminOrderListItemDto>>($"/api/v1/admin/orders?search={order.OrderNumber}", HttpExtensions.Json);
        byNumber!.Items.ShouldHaveSingleItem().Id.ShouldBe(order.Id);

        var pending = await admin.GetFromJsonAsync<PagedResult<AdminOrderListItemDto>>("/api/v1/admin/orders?status=Pending&pageSize=100", HttpExtensions.Json);
        pending!.Items.ShouldAllBe(o => o.Status == OrderStatus.Pending);
    }

    [Fact]
    public async Task Inventory_adjustments_are_audited_and_cannot_go_negative()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(stock: 4);
        var variant = product.Variants[0];

        var restock = await admin.PostAsJsonAsync($"/api/v1/admin/inventory/{variant.Id}/adjust", new AdjustStockRequest(20, InventoryTransactionType.Restock, "New batch"));
        (await restock.Content.ReadFromJsonAsync<InventoryItemDto>(HttpExtensions.Json))!.StockQuantity.ShouldBe(24);

        var tooMuch = await admin.PostAsJsonAsync($"/api/v1/admin/inventory/{variant.Id}/adjust", new AdjustStockRequest(-25, InventoryTransactionType.Adjustment, null));
        (await tooMuch.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("stock_negative");

        (await admin.PostAsJsonAsync($"/api/v1/admin/inventory/{variant.Id}/adjust", new AdjustStockRequest(5, InventoryTransactionType.Sale, null)))
            .StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        var tx = await admin.GetFromJsonAsync<PagedResult<InventoryTransactionDto>>($"/api/v1/admin/inventory/{variant.Id}/transactions", HttpExtensions.Json);
        tx!.Items.Select(t => t.Type).ShouldBe([InventoryTransactionType.Restock, InventoryTransactionType.Restock]); // newest first: adjust, initial
        tx.Items[0].StockAfter.ShouldBe(24);

        (await admin.PostAsJsonAsync($"/api/v1/admin/inventory/{variant.Id}/adjust", new AdjustStockRequest(-22, InventoryTransactionType.Adjustment, "Count"))).EnsureSuccessStatusCode();
        var low = await admin.GetFromJsonAsync<PagedResult<InventoryItemDto>>("/api/v1/admin/inventory?lowStockOnly=true&pageSize=100", HttpExtensions.Json);
        low!.Items.ShouldContain(i => i.VariantId == variant.Id && i.IsLowStock);
    }
}
