using HoneyHouse.Application.Features.Analytics;
using HoneyHouse.Application.Features.Inventory;
using HoneyHouse.Application.Features.Orders;

namespace HoneyHouse.Admin.Models;

public sealed record DashboardViewModel
{
    public DateOnly From { get; init; }
    public DateOnly To { get; init; }
    public int? Days { get; init; }
    public bool ShowSales { get; init; }
    public string? Error { get; init; }

    public SalesSummaryDto? Summary { get; init; }
    public IReadOnlyList<SalesPointDto> Sales { get; init; } = [];
    public bool SalesMonthly { get; init; }
    public IReadOnlyList<TopProductDto> TopProducts { get; init; } = [];
    public IReadOnlyList<GovernorateSalesDto> Governorates { get; init; } = [];

    public IReadOnlyList<StatusCountDto> OrdersByStatus { get; init; } = [];
    public int PendingOrders { get; init; }
    public int LowStockCount { get; init; }
    public IReadOnlyList<AdminOrderListItemDto> RecentOrders { get; init; } = [];
    public IReadOnlyList<InventoryItemDto> LowStock { get; init; } = [];
}
