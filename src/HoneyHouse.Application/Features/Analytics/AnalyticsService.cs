using HoneyHouse.Application.Common;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Localization;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Application.Features.Analytics;

public sealed record AnalyticsQuery(DateTime? From = null, DateTime? To = null);

public sealed record SalesSummaryDto(DateTime From, DateTime To, decimal Revenue, decimal PaidRevenue, int Orders, decimal AverageOrderValue,
    int ItemsSold, int CancelledOrders, int NewCustomers, int PendingOrdersNow, int LowStockVariants, decimal DiscountsGiven, string Currency);

public sealed record SalesPointDto(string Period, int Orders, decimal Revenue);
public sealed record TopProductDto(int ProductId, string NameAr, string NameEn, int QuantitySold, decimal Revenue);
public sealed record StatusCountDto(OrderStatus Status, string StatusName, int Count);
public sealed record GovernorateSalesDto(string Governorate, int Orders, decimal Revenue);

public interface IAnalyticsService
{
    Task<Result<SalesSummaryDto>> SummaryAsync(AnalyticsQuery query, CancellationToken ct = default);
    Task<Result<IReadOnlyList<SalesPointDto>>> SalesAsync(AnalyticsQuery query, string? groupBy, CancellationToken ct = default);
    Task<Result<IReadOnlyList<TopProductDto>>> TopProductsAsync(AnalyticsQuery query, int limit, CancellationToken ct = default);
    Task<IReadOnlyList<StatusCountDto>> OrdersByStatusAsync(CancellationToken ct = default);
    Task<Result<IReadOnlyList<GovernorateSalesDto>>> SalesByGovernorateAsync(AnalyticsQuery query, CancellationToken ct = default);
}

public sealed class AnalyticsService(IAnalyticsRepository data, ILocalizer l, TimeProvider clock) : IAnalyticsService
{
    /// <summary>Reports are grouped by the store's local (Cairo) calendar.</summary>
    public static readonly TimeZoneInfo StoreTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Cairo");

    public async Task<Result<SalesSummaryDto>> SummaryAsync(AnalyticsQuery q, CancellationToken ct = default)
    {
        var range = Range(q);
        if (range.IsFailure) return Result<SalesSummaryDto>.Failure(range.Errors);
        var (from, to) = range.Value;

        var orders = await data.GetOrdersAsync(from, to, ct);
        var counted = orders.Where(o => IsRevenue(o.Status)).ToList();
        var revenue = counted.Sum(o => o.Total);
        return new SalesSummaryDto(from, to, revenue, counted.Where(o => o.PaymentStatus == PaymentStatus.Paid).Sum(o => o.Total),
            counted.Count, counted.Count == 0 ? 0 : Math.Round(revenue / counted.Count, 2), counted.Sum(o => o.ItemCount),
            orders.Count(o => o.Status == OrderStatus.Cancelled), await data.CountNewCustomersAsync(from, to, ct),
            await data.CountOrdersInStatusAsync(OrderStatus.Pending, ct), await data.CountLowStockVariantsAsync(ct),
            counted.Sum(o => o.Discount), "EGP");
    }

    public async Task<Result<IReadOnlyList<SalesPointDto>>> SalesAsync(AnalyticsQuery q, string? groupBy, CancellationToken ct = default)
    {
        var range = Range(q);
        if (range.IsFailure) return Result<IReadOnlyList<SalesPointDto>>.Failure(range.Errors);
        var (from, to) = range.Value;
        var monthly = string.Equals(groupBy, "month", StringComparison.OrdinalIgnoreCase);

        var orders = (await data.GetOrdersAsync(from, to, ct)).Where(o => IsRevenue(o.Status)).ToList();
        string Key(DateTime utc)
        {
            var local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), StoreTimeZone);
            return monthly ? local.ToString("yyyy-MM") : local.ToString("yyyy-MM-dd");
        }

        var grouped = orders.GroupBy(o => Key(o.CreatedAt)).ToDictionary(g => g.Key, g => (Count: g.Count(), Sum: g.Sum(o => o.Total)));

        // Fill empty periods so charts have a continuous axis.
        var points = new List<SalesPointDto>();
        var cursor = TimeZoneInfo.ConvertTimeFromUtc(from, StoreTimeZone).Date;
        var end = TimeZoneInfo.ConvertTimeFromUtc(to.AddTicks(-1), StoreTimeZone).Date;
        if (monthly) { cursor = new DateTime(cursor.Year, cursor.Month, 1); end = new DateTime(end.Year, end.Month, 1); }
        while (cursor <= end)
        {
            var key = monthly ? cursor.ToString("yyyy-MM") : cursor.ToString("yyyy-MM-dd");
            var v = grouped.GetValueOrDefault(key);
            points.Add(new SalesPointDto(key, v.Count, v.Sum));
            cursor = monthly ? cursor.AddMonths(1) : cursor.AddDays(1);
        }
        return points;
    }

    public async Task<Result<IReadOnlyList<TopProductDto>>> TopProductsAsync(AnalyticsQuery q, int limit, CancellationToken ct = default)
    {
        var range = Range(q);
        if (range.IsFailure) return Result<IReadOnlyList<TopProductDto>>.Failure(range.Errors);
        var rows = await data.GetTopProductsAsync(range.Value.From, range.Value.To, Math.Clamp(limit, 1, 50), ct);
        return rows.Select(r => new TopProductDto(r.ProductId, r.NameAr, r.NameEn, r.Quantity, r.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<StatusCountDto>> OrdersByStatusAsync(CancellationToken ct = default)
    {
        var counts = await data.CountOrdersByStatusAsync(ct);
        return Enum.GetValues<OrderStatus>().Select(s => new StatusCountDto(s, l[$"status_{s}"], counts.GetValueOrDefault(s))).ToList();
    }

    public async Task<Result<IReadOnlyList<GovernorateSalesDto>>> SalesByGovernorateAsync(AnalyticsQuery q, CancellationToken ct = default)
    {
        var range = Range(q);
        if (range.IsFailure) return Result<IReadOnlyList<GovernorateSalesDto>>.Failure(range.Errors);
        var orders = (await data.GetOrdersAsync(range.Value.From, range.Value.To, ct)).Where(o => IsRevenue(o.Status));
        return orders.GroupBy(o => o.Governorate)
            .Select(g => new GovernorateSalesDto(g.Key, g.Count(), g.Sum(o => o.Total)))
            .OrderByDescending(g => g.Revenue).ToList();
    }

    public static bool IsRevenue(OrderStatus s) => s is not (OrderStatus.Cancelled or OrderStatus.Returned);

    private Result<(DateTime From, DateTime To)> Range(AnalyticsQuery q)
    {
        var to = (q.To ?? clock.GetUtcNow().UtcDateTime).ToUniversalTime();
        var from = (q.From ?? to.AddDays(-30)).ToUniversalTime();
        if (from >= to || to - from > TimeSpan.FromDays(731)) return Error.Validation("date_range_invalid", "from");
        return (from, to);
    }
}
