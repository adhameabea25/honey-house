using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HoneyHouse.Infrastructure.Persistence.Repositories;

public sealed class AnalyticsRepository(AppDbContext db) : IAnalyticsRepository
{
    public async Task<IReadOnlyList<OrderFact>> GetOrdersAsync(DateTime from, DateTime to, CancellationToken ct = default) =>
        await db.Orders.AsNoTracking().Where(o => o.CreatedAt >= from && o.CreatedAt < to)
            .Select(o => new OrderFact(o.Id, o.CreatedAt, o.Status, o.PaymentStatus, o.Total, o.DiscountAmount,
                o.Items.Sum(i => (int?)i.Quantity) ?? 0, o.ShipGovernorateName))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ProductSalesRow>> GetTopProductsAsync(DateTime from, DateTime to, int limit, CancellationToken ct = default)
    {
        var top = await db.OrderItems.AsNoTracking()
            .Where(i => i.Order.CreatedAt >= from && i.Order.CreatedAt < to
                        && i.Order.Status != OrderStatus.Cancelled && i.Order.Status != OrderStatus.Returned)
            .GroupBy(i => i.ProductId)
            .Select(g => new { ProductId = g.Key, Quantity = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.LineTotal) })
            .OrderByDescending(r => r.Quantity).ThenByDescending(r => r.Revenue)
            .Take(limit).ToListAsync(ct);

        var ids = top.Select(t => t.ProductId).ToList();
        // Include soft-deleted products so historical best-sellers keep their names.
        var names = await db.Products.IgnoreQueryFilters().AsNoTracking().Where(p => ids.Contains(p.Id))
            .Select(p => new { p.Id, p.NameAr, p.NameEn }).ToDictionaryAsync(p => p.Id, ct);

        return top.Select(t => new ProductSalesRow(t.ProductId,
            names.TryGetValue(t.ProductId, out var n) ? n.NameAr : string.Empty,
            names.TryGetValue(t.ProductId, out var m) ? m.NameEn : string.Empty,
            t.Quantity, t.Revenue)).ToList();
    }

    public async Task<IReadOnlyDictionary<OrderStatus, int>> CountOrdersByStatusAsync(CancellationToken ct = default) =>
        await db.Orders.AsNoTracking().GroupBy(o => o.Status).Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

    public Task<int> CountOrdersInStatusAsync(OrderStatus status, CancellationToken ct = default) =>
        db.Orders.CountAsync(o => o.Status == status, ct);

    public Task<int> CountNewCustomersAsync(DateTime from, DateTime to, CancellationToken ct = default)
    {
        var customerRoleId = db.Roles.Where(r => r.Name == Roles.Customer).Select(r => r.Id);
        return db.Users.CountAsync(u => u.CreatedAt >= from && u.CreatedAt < to
                                        && db.UserRoles.Any(ur => ur.UserId == u.Id && customerRoleId.Contains(ur.RoleId)), ct);
    }

    public Task<int> CountLowStockVariantsAsync(CancellationToken ct = default) =>
        db.ProductVariants.CountAsync(v => v.IsActive && !v.Product.IsDeleted && v.Product.IsActive && v.StockQuantity <= v.LowStockThreshold, ct);
}
