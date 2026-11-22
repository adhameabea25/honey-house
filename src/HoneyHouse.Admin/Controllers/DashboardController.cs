using HoneyHouse.Admin.Common;
using HoneyHouse.Admin.Models;
using HoneyHouse.Application.Features.Analytics;
using HoneyHouse.Application.Features.Inventory;
using HoneyHouse.Application.Features.Orders;
using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace HoneyHouse.Admin.Controllers;

public sealed class DashboardController(
    IAnalyticsService analytics, IOrderService orders, IInventoryService inventory, TimeProvider clock) : AdminControllerBase
{
    /// <param name="days">Quick range (7, 30, 90, 365) ending today; ignored when from/to are given.</param>
    public async Task<IActionResult> Index(int? days, DateOnly? from, DateOnly? to, CancellationToken ct)
    {
        var today = Fmt.Today(clock);
        var end = to ?? today;
        var start = from ?? end.AddDays(-((days is > 0 and <= 731 ? days.Value : 30) - 1));
        if (from is not null || to is not null) days = null; else days ??= 30;

        var lowStock = await inventory.SearchAsync(new InventoryQuery(LowStockOnly: true, PageSize: 8), ct);
        var recent = await orders.SearchAsync(new AdminOrderQuery(PageSize: 8), ct);
        var byStatus = await analytics.OrdersByStatusAsync(ct);

        var model = new DashboardViewModel
        {
            From = start, To = end, Days = days,
            ShowSales = User.IsInRole(Roles.Admin),
            OrdersByStatus = byStatus,
            PendingOrders = byStatus.FirstOrDefault(s => s.Status == OrderStatus.Pending)?.Count ?? 0,
            LowStockCount = lowStock.TotalCount,
            LowStock = lowStock.Items,
            RecentOrders = recent.Items
        };

        // Sales figures are Admin-only, as in the API.
        if (!model.ShowSales) return View(model);

        var query = new AnalyticsQuery(Fmt.LocalDateToUtc(start), Fmt.LocalDateToUtc(end.AddDays(1)));
        var monthly = end.DayNumber - start.DayNumber > 92;
        var summary = await analytics.SummaryAsync(query, ct);
        if (summary.IsFailure) return View(model with { Error = T[summary.Errors[0].Code] });

        var sales = await analytics.SalesAsync(query, monthly ? "month" : "day", ct);
        var top = await analytics.TopProductsAsync(query, 5, ct);
        var gov = await analytics.SalesByGovernorateAsync(query, ct);

        return View(model with
        {
            Summary = summary.Value, Sales = sales.Value, SalesMonthly = monthly,
            TopProducts = top.Value, Governorates = gov.Value.Take(8).ToList()
        });
    }
}
