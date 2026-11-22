using System.Net;
using System.Text;
using HoneyHouse.Admin.Tests.Infrastructure;
using HoneyHouse.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HoneyHouse.Admin.Tests;

[Collection(AdminCollection.Name)]
public class OrderTests(AdminAppFactory factory)
{
    private static Dictionary<string, string> Status(string status, string? note = null, string? tracking = null) => new()
    {
        ["Form.Status"] = status, ["Form.Note"] = note ?? "", ["Form.TrackingNumber"] = tracking ?? ""
    };

    [Fact]
    public async Task Order_moves_through_lifecycle_and_cod_is_paid_on_delivery()
    {
        var product = await factory.CreateProductAsync(price: 120, stock: 10);
        var placed = await factory.PlaceOrderAsync(product.Variants[0].Id, 2);
        var id = placed.Order.Id;
        var client = await factory.SignedInAsync();
        var url = $"/Orders/Details/{id}";

        var html = await client.GetPageAsync(url);
        html.ShouldContain(placed.Order.OrderNumber);
        html.ShouldContain("قيد المراجعة");
        html.ShouldContain("محمد أحمد"); // recipient

        foreach (var (status, tracking) in new[] { ("Confirmed", (string?)null), ("Processing", null), ("Shipped", "BOSTA-123"), ("OutForDelivery", null), ("Delivered", null) })
        {
            var r = await client.SubmitAsync(url, $"/Orders/UpdateStatus/{id}", Status(status, $"note {status}", tracking));
            r.StatusCode.ShouldBe(HttpStatusCode.Redirect, $"{status}: {await r.Content.ReadAsStringAsync()}");
        }

        html = await client.GetPageAsync(url);
        html.ShouldContain("data-testid=\"order-status\">تم التوصيل<");
        html.ShouldContain("data-testid=\"payment-status\">مدفوع<");
        html.ShouldContain("BOSTA-123");
        html.ShouldContain("note Shipped");

        await factory.WithDbAsync(async db =>
        {
            var o = await db.Orders.Include(x => x.StatusHistory).SingleAsync(x => x.Id == id);
            o.Status.ShouldBe(OrderStatus.Delivered);
            o.PaymentStatus.ShouldBe(PaymentStatus.Paid);
            o.StatusHistory.Count.ShouldBe(6);
            o.StatusHistory.Where(h => h.ToStatus != OrderStatus.Pending).ShouldAllBe(h => h.ChangedByUserId != null);
        });
    }

    [Fact]
    public async Task Invalid_transition_is_refused_with_message()
    {
        var product = await factory.CreateProductAsync();
        var placed = await factory.PlaceOrderAsync(product.Variants[0].Id);
        var client = await factory.SignedInAsync();
        var r = await client.SubmitAsync($"/Orders/Details/{placed.Order.Id}", $"/Orders/UpdateStatus/{placed.Order.Id}", Status("Delivered"));
        r.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await r.Content.ReadAsStringAsync()).ShouldContain("لا يمكن تغيير حالة الطلب من قيد المراجعة إلى تم التوصيل");
    }

    [Fact]
    public async Task Cancelling_returns_stock()
    {
        var product = await factory.CreateProductAsync(stock: 10);
        var placed = await factory.PlaceOrderAsync(product.Variants[0].Id, 3);
        var client = await factory.SignedInAsync();
        var r = await client.SubmitAsync($"/Orders/Details/{placed.Order.Id}", $"/Orders/UpdateStatus/{placed.Order.Id}", Status("Cancelled", "العميل اعتذر"));
        r.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        await factory.WithDbAsync(async db =>
            (await db.ProductVariants.SingleAsync(v => v.Id == product.Variants[0].Id)).StockQuantity.ShouldBe(10));
        (await client.GetPageAsync($"/Orders/Details/{placed.Order.Id}")).ShouldContain("العميل اعتذر");
    }

    [Fact]
    public async Task Mark_paid_records_payment_once()
    {
        var product = await factory.CreateProductAsync();
        var placed = await factory.PlaceOrderAsync(product.Variants[0].Id);
        var client = await factory.SignedInAsync();
        var url = $"/Orders/Details/{placed.Order.Id}";
        (await client.SubmitAsync(url, $"/Orders/MarkPaid/{placed.Order.Id}", new { })).StatusCode.ShouldBe(HttpStatusCode.Redirect);
        var html = await client.GetPageAsync(url);
        html.ShouldContain("تم تسجيل الدفع");
        html.ShouldNotContain("/Orders/MarkPaid/"); // button hidden once paid
    }

    [Fact]
    public async Task List_filters_by_status_and_search()
    {
        var product = await factory.CreateProductAsync();
        var placed = await factory.PlaceOrderAsync(product.Variants[0].Id);
        var client = await factory.SignedInAsync();

        (await client.GetPageAsync("/Orders?Status=Pending&Search=" + placed.Order.OrderNumber)).ShouldContain(placed.Order.OrderNumber);
        (await client.GetPageAsync("/Orders?Status=Delivered&Search=" + placed.Order.OrderNumber)).ShouldNotContain($"/Orders/Details/{placed.Order.Id}\"");
        var today = DateTime.UtcNow.AddHours(2).ToString("yyyy-MM-dd");
        (await client.GetPageAsync($"/Orders?From={today}&To={today}&PaymentMethod=CashOnDelivery")).ShouldContain("data-testid=\"orders\"");
    }

    [Fact]
    public async Task Export_returns_csv_with_bom_and_arabic_headers()
    {
        var product = await factory.CreateProductAsync();
        var placed = await factory.PlaceOrderAsync(product.Variants[0].Id);
        var client = await factory.SignedInAsync();
        var response = await client.GetAsync("/Orders/Export?Search=" + placed.Order.OrderNumber);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/csv");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Take(3).ShouldBe(Encoding.UTF8.GetPreamble());
        var text = Encoding.UTF8.GetString(bytes);
        text.ShouldContain("رقم الطلب");
        text.ShouldContain(placed.Order.OrderNumber);
        text.ShouldContain("الدفع عند الاستلام");
    }

    [Fact]
    public async Task Print_view_shows_amount_to_collect()
    {
        var product = await factory.CreateProductAsync(price: 99);
        var placed = await factory.PlaceOrderAsync(product.Variants[0].Id);
        var client = await factory.SignedInAsync();
        var html = await client.GetPageAsync($"/Orders/Print/{placed.Order.Id}");
        html.ShouldContain("المطلوب تحصيله");
        html.ShouldContain(placed.Order.OrderNumber);
    }

    [Fact]
    public async Task Unknown_order_is_404()
    {
        var client = await factory.SignedInAsync();
        await client.GetPageAsync("/Orders/Details/999999", HttpStatusCode.NotFound);
    }
}
