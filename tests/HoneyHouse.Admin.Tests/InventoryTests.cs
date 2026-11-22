using System.Net;
using HoneyHouse.Admin.Tests.Infrastructure;
using HoneyHouse.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HoneyHouse.Admin.Tests;

[Collection(AdminCollection.Name)]
public class InventoryTests(AdminAppFactory factory)
{
    [Fact]
    public async Task Restock_and_adjustment_update_stock_and_history()
    {
        var product = await factory.CreateProductAsync(stock: 10, nameAr: "عسل مخزون");
        var variantId = product.Variants[0].Id;
        var client = await factory.SignedInAsync();
        var url = $"/Inventory/Variant/{variantId}";

        var restock = await client.SubmitAsync(url, url, new Dictionary<string, string>
            { ["Form.Type"] = "Restock", ["Form.QuantityChange"] = "15", ["Form.Note"] = "توريد من المنحل" });
        restock.StatusCode.ShouldBe(HttpStatusCode.Redirect, await restock.Content.ReadAsStringAsync());

        var damaged = await client.SubmitAsync(url, url, new Dictionary<string, string>
            { ["Form.Type"] = "Adjustment", ["Form.QuantityChange"] = "-3", ["Form.Note"] = "عبوات تالفة" });
        damaged.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        var html = await client.GetPageAsync(url);
        html.ShouldContain("data-testid=\"current-stock\">22<");
        html.ShouldContain("توريد من المنحل");
        html.ShouldContain("عبوات تالفة");
        html.ShouldContain("Honey House Admin"); // staff name recorded

        await factory.WithDbAsync(async db =>
        {
            var txs = await db.InventoryTransactions.Where(t => t.VariantId == variantId).OrderBy(t => t.Id).ToListAsync();
            txs.Select(t => t.StockAfter).ShouldBe([10, 25, 22]);
            txs[1].CreatedByUserId.ShouldNotBeNull();
        });
    }

    [Fact]
    public async Task Stock_cannot_go_below_zero()
    {
        var product = await factory.CreateProductAsync(stock: 2);
        var client = await factory.SignedInAsync();
        var url = $"/Inventory/Variant/{product.Variants[0].Id}";
        var response = await client.SubmitAsync(url, url, new Dictionary<string, string> { ["Form.Type"] = "Adjustment", ["Form.QuantityChange"] = "-5" });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("لا يمكن أن يصبح المخزون أقل من صفر");
    }

    [Fact]
    public async Task Restock_must_be_positive_and_not_zero()
    {
        var product = await factory.CreateProductAsync();
        var client = await factory.SignedInAsync();
        var url = $"/Inventory/Variant/{product.Variants[0].Id}";
        var response = await client.SubmitAsync(url, url, new Dictionary<string, string> { ["Form.Type"] = "Restock", ["Form.QuantityChange"] = "-1" });
        (await response.Content.ReadAsStringAsync()).ShouldContain("يجب أن تكون القيمة أكبر من 0");
        var zero = await client.SubmitAsync(url, url, new Dictionary<string, string> { ["Form.Type"] = "Adjustment", ["Form.QuantityChange"] = "0" });
        zero.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Sale_type_cannot_be_posted_manually()
    {
        var product = await factory.CreateProductAsync();
        var client = await factory.SignedInAsync();
        var url = $"/Inventory/Variant/{product.Variants[0].Id}";
        var response = await client.SubmitAsync(url, url, new Dictionary<string, string> { ["Form.Type"] = "Sale", ["Form.QuantityChange"] = "-1" });
        (await response.Content.ReadAsStringAsync()).ShouldContain("نوع الحركة يجب أن يكون");
    }

    [Fact]
    public async Task Sales_appear_in_history_with_order_link()
    {
        var product = await factory.CreateProductAsync(stock: 5);
        var order = await factory.PlaceOrderAsync(product.Variants[0].Id, 2);
        var client = await factory.SignedInAsync();
        var html = await client.GetPageAsync($"/Inventory/Variant/{product.Variants[0].Id}");
        html.ShouldContain(order.Order.OrderNumber);
        html.ShouldContain("بيع");
    }

    [Fact]
    public async Task List_filters_low_stock_and_search()
    {
        var low = await factory.CreateProductAsync(stock: 1, nameAr: "عسل منخفض جدا");
        var client = await factory.SignedInAsync();
        var html = await client.GetPageAsync("/Inventory?lowStockOnly=true&search=" + Uri.EscapeDataString(low.Variants[0].Sku));
        html.ShouldContain("عسل منخفض جدا");
        (await client.GetPageAsync("/Inventory?search=no-such-sku-xyz")).ShouldContain("لا توجد نتائج");
    }

    [Fact]
    public async Task Unknown_variant_is_404()
    {
        var client = await factory.SignedInAsync();
        await client.GetPageAsync("/Inventory/Variant/999999", HttpStatusCode.NotFound);
    }
}
