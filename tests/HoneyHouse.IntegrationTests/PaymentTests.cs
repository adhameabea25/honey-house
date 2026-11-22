using System.Net;
using System.Net.Http.Json;
using System.Text;
using HoneyHouse.Application.Features.Cart;
using HoneyHouse.Application.Features.Catalog;
using HoneyHouse.Application.Features.Orders;
using HoneyHouse.Application.Features.Payments;
using HoneyHouse.Domain.Enums;
using HoneyHouse.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace HoneyHouse.IntegrationTests;

// Shares FakePaymobHandler.Fail, so these tests must not run in parallel with each other (same collection => sequential).
[Collection(ApiCollection.Name)]
public class PaymentTests(HoneyHouseApiFactory factory)
{
    private sealed record Placed(HttpClient Client, OrderPlacedDto Result, AdminProductDto Product, string ProviderOrderId);

    private async Task<Placed> PlaceOnlineOrderAsync(int qty = 2, decimal price = 100)
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(price: price, stock: 10);
        var (client, _) = await factory.CreateCustomerClientAsync();
        var address = await client.CreateAddressAsync(1);
        (await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(product.Variants[0].Id, qty))).EnsureSuccessStatusCode();

        var res = await client.PostAsJsonAsync("/api/v1/orders", new PlaceOrderRequest(address.Id, PaymentMethod.Online, null, null));
        res.StatusCode.ShouldBe(HttpStatusCode.Created, await res.Content.ReadAsStringAsync());
        var placed = (await res.Content.ReadFromJsonAsync<OrderPlacedDto>(HttpExtensions.Json))!;
        var providerOrderId = placed.PaymentUrl!.Split("cs_test_")[1];
        return new Placed(client, placed, product, providerOrderId);
    }

    private Task<HttpResponseMessage> PostCallbackAsync(string body, string hmac) =>
        factory.CreateClient().PostAsync($"/api/v1/payments/paymob/webhook?hmac={hmac}", new StringContent(body, Encoding.UTF8, "application/json"));

    private async Task<OrderDetailDto> GetOrderAsync(Placed p) =>
        (await p.Client.GetFromJsonAsync<OrderDetailDto>($"/api/v1/orders/{p.Result.Order.Id}", HttpExtensions.Json))!;

    [Fact]
    public async Task Online_order_returns_paymob_checkout_url()
    {
        var p = await PlaceOnlineOrderAsync(qty: 2, price: 100);
        p.Result.PaymentUrl!.ShouldStartWith("https://accept.paymob.com/unifiedcheckout/?publicKey=pk_test&clientSecret=cs_test_");
        p.Result.Order.PaymentMethod.ShouldBe(PaymentMethod.Online);
        p.Result.Order.PaymentStatus.ShouldBe(PaymentStatus.Pending);

        var request = factory.Paymob.Requests.Last(r => r.Body["special_reference"]!.GetValue<string>() == p.Result.Order.OrderNumber);
        request.Auth.ShouldBe("Token sk_test");
        request.Body["amount"]!.GetValue<long>().ShouldBe((long)(p.Result.Order.Total * 100)); // 200 + 50 shipping = 25000 cents
        request.Body["currency"]!.GetValue<string>().ShouldBe("EGP");
        request.Body["payment_methods"]![0]!.GetValue<int>().ShouldBe(4567);
    }

    [Fact]
    public async Task Successful_callback_marks_order_paid_and_confirmed_idempotently()
    {
        var p = await PlaceOnlineOrderAsync();
        var (body, hmac) = PaymobCallback.Create(p.ProviderOrderId, p.Result.Order.OrderNumber, p.Result.Order.Total, success: true, txId: 5550001);

        (await PostCallbackAsync(body, hmac)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await PostCallbackAsync(body, hmac)).StatusCode.ShouldBe(HttpStatusCode.OK); // duplicate delivery

        var order = await GetOrderAsync(p);
        order.PaymentStatus.ShouldBe(PaymentStatus.Paid);
        order.Status.ShouldBe(OrderStatus.Confirmed);
        order.History.Count(h => h.Status == OrderStatus.Confirmed).ShouldBe(1);

        // A later "failed" notification must not downgrade a paid order
        var (failBody, failHmac) = PaymobCallback.Create(p.ProviderOrderId, p.Result.Order.OrderNumber, p.Result.Order.Total, success: false);
        (await PostCallbackAsync(failBody, failHmac)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetOrderAsync(p)).PaymentStatus.ShouldBe(PaymentStatus.Paid);
    }

    [Fact]
    public async Task Callback_with_bad_signature_is_rejected()
    {
        var p = await PlaceOnlineOrderAsync();
        var (body, _) = PaymobCallback.Create(p.ProviderOrderId, p.Result.Order.OrderNumber, p.Result.Order.Total, success: true);
        (await PostCallbackAsync(body, new string('0', 128))).StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await PostCallbackAsync(body.Replace("\"success\":true", "\"success\":false"), PaymobCallback.Create(p.ProviderOrderId, p.Result.Order.OrderNumber, p.Result.Order.Total, true).Hmac))
            .StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        (await GetOrderAsync(p)).PaymentStatus.ShouldBe(PaymentStatus.Pending);
    }

    [Fact]
    public async Task Failed_payment_can_be_retried_then_succeed()
    {
        var p = await PlaceOnlineOrderAsync();
        var (failBody, failHmac) = PaymobCallback.Create(p.ProviderOrderId, p.Result.Order.OrderNumber, p.Result.Order.Total, success: false);
        (await PostCallbackAsync(failBody, failHmac)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetOrderAsync(p)).PaymentStatus.ShouldBe(PaymentStatus.Failed);

        var retry = await p.Client.PostAsync($"/api/v1/payments/{p.Result.Order.Id}/retry", null);
        retry.StatusCode.ShouldBe(HttpStatusCode.OK);
        var session = (await retry.Content.ReadFromJsonAsync<PaymentSessionDto>(HttpExtensions.Json))!;
        var newProviderId = session.PaymentUrl.Split("cs_test_")[1];
        newProviderId.ShouldNotBe(p.ProviderOrderId);

        var (okBody, okHmac) = PaymobCallback.Create(newProviderId, p.Result.Order.OrderNumber, p.Result.Order.Total, success: true);
        (await PostCallbackAsync(okBody, okHmac)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetOrderAsync(p)).PaymentStatus.ShouldBe(PaymentStatus.Paid);

        (await p.Client.PostAsync($"/api/v1/payments/{p.Result.Order.Id}/retry", null)).StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Underpaid_callback_is_treated_as_failure()
    {
        var p = await PlaceOnlineOrderAsync();
        var (body, hmac) = PaymobCallback.Create(p.ProviderOrderId, p.Result.Order.OrderNumber, 1m, success: true);
        (await PostCallbackAsync(body, hmac)).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await GetOrderAsync(p)).PaymentStatus.ShouldBe(PaymentStatus.Failed);
    }

    [Fact]
    public async Task Unpaid_online_orders_expire_and_release_stock()
    {
        var p = await PlaceOnlineOrderAsync(qty: 3);
        await factory.WithDbAsync(async db =>
        {
            var order = await db.Orders.SingleAsync(o => o.Id == p.Result.Order.Id);
            order.PaymentDueAt = DateTime.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        });

        using (var scope = factory.Services.CreateScope())
            (await scope.ServiceProvider.GetRequiredService<IPaymentService>().ExpireUnpaidOrdersAsync()).ShouldBeGreaterThanOrEqualTo(1);

        var order = await GetOrderAsync(p);
        order.Status.ShouldBe(OrderStatus.Cancelled);
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.GetFromJsonAsync<AdminProductDto>($"/api/v1/admin/products/{p.Product.Id}", HttpExtensions.Json);
        product!.Variants[0].StockQuantity.ShouldBe(10);
    }

    [Fact]
    public async Task Gateway_failure_rolls_back_the_whole_order()
    {
        var admin = await factory.CreateAdminClientAsync();
        var product = await admin.CreateProductAsync(stock: 10);
        var (client, _) = await factory.CreateCustomerClientAsync();
        var address = await client.CreateAddressAsync(1);
        await client.PostAsJsonAsync("/api/v1/cart/items", new AddCartItemRequest(product.Variants[0].Id, 2));

        factory.Paymob.Fail = true;
        try
        {
            var res = await client.PostAsJsonAsync("/api/v1/orders", new PlaceOrderRequest(address.Id, PaymentMethod.Online, null, null));
            res.StatusCode.ShouldBe(HttpStatusCode.UnprocessableEntity);
            (await res.ReadJsonAsync()).GetProperty("code").GetString().ShouldBe("payment_gateway_error");
        }
        finally
        {
            factory.Paymob.Fail = false;
        }

        (await client.GetFromJsonAsync<CartDto>("/api/v1/cart", HttpExtensions.Json))!.Items.ShouldHaveSingleItem().Quantity.ShouldBe(2);
        var after = await admin.GetFromJsonAsync<AdminProductDto>($"/api/v1/admin/products/{product.Id}", HttpExtensions.Json);
        after!.Variants[0].StockQuantity.ShouldBe(10);
        (await client.GetFromJsonAsync<HoneyHouse.Application.Common.PagedResult<OrderSummaryDto>>("/api/v1/orders", HttpExtensions.Json))!.Items.ShouldBeEmpty();
    }
}
