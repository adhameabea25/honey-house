using HoneyHouse.Admin.Tests.Infrastructure;
using HoneyHouse.Domain.Common;

namespace HoneyHouse.Admin.Tests;

[Collection(AdminCollection.Name)]
public class DashboardTests(AdminAppFactory factory)
{
    [Fact]
    public async Task Admin_sees_sales_kpis_charts_and_recent_orders()
    {
        var product = await factory.CreateProductAsync(price: 150, nameAr: "عسل لوحة التحكم");
        var order = await factory.PlaceOrderAsync(product.Variants[0].Id, quantity: 2);

        var client = await factory.SignedInAsync();
        var html = await client.GetPageAsync("/");

        html.ShouldContain("data-kpi=\"kpi_revenue\"");
        html.ShouldContain("id=\"salesChart\"");
        html.ShouldContain("chart.umd.js");
        html.ShouldContain(order.Order.OrderNumber);
        html.ShouldContain("عسل لوحة التحكم"); // top products
        html.ShouldContain("ج.م");
    }

    [Fact]
    public async Task Manager_sees_operations_but_not_sales()
    {
        var email = await factory.CreateUserAsync(Roles.Manager);
        var client = await factory.SignedInAsync(email);
        var html = await client.GetPageAsync("/");

        html.ShouldContain("data-testid=\"pending-count\"");
        html.ShouldContain("data-testid=\"recent-orders\"");
        html.ShouldNotContain("data-kpi=\"kpi_revenue\"");
        html.ShouldNotContain("id=\"salesChart\"");
    }

    [Fact]
    public async Task Low_stock_variant_is_listed()
    {
        await factory.CreateProductAsync(stock: 1, nameAr: "عسل قارب على النفاد", lowStockThreshold: 5);
        var client = await factory.SignedInAsync();
        var html = await client.GetPageAsync("/");
        html.ShouldContain("عسل قارب على النفاد");
    }

    [Theory]
    [InlineData("/?days=7")]
    [InlineData("/?days=365")]
    [InlineData("/?from=2026-01-01&to=2026-03-31")]
    public async Task Date_ranges_render(string url)
    {
        var client = await factory.SignedInAsync();
        var html = await client.GetPageAsync(url);
        html.ShouldContain("data-kpi=\"kpi_orders\"");
    }

    [Fact]
    public async Task Long_range_switches_to_monthly_chart()
    {
        var client = await factory.SignedInAsync();
        var html = await client.GetPageAsync("/?days=365");
        html.ShouldContain("المبيعات الشهرية");
    }

    [Fact]
    public async Task Invalid_range_shows_localized_error()
    {
        var client = await factory.SignedInAsync();
        var html = await client.GetPageAsync("/?from=2026-05-01&to=2026-01-01");
        html.ShouldContain("نطاق التاريخ غير صالح");
    }
}
