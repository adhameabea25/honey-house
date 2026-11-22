using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Domain.Entities;
using HoneyHouse.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HoneyHouse.Infrastructure.Persistence.Repositories;

public sealed class AddressRepository(AppDbContext db) : GenericRepository<Address>(db), IAddressRepository
{
    public async Task<IReadOnlyList<Address>> GetForUserAsync(string userId, CancellationToken ct = default) =>
        await Set.Include(a => a.Governorate).Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault).ThenBy(a => a.Id).ToListAsync(ct);

    public Task<Address?> GetForUserAsync(string userId, int id, CancellationToken ct = default) =>
        Set.Include(a => a.Governorate).FirstOrDefaultAsync(a => a.UserId == userId && a.Id == id, ct);
}

public sealed class CartRepository(AppDbContext db) : GenericRepository<Cart>(db), ICartRepository
{
    public Task<Cart?> GetWithItemsAsync(string userId, CancellationToken ct = default) =>
        Set.Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product).ThenInclude(p => p.Images)
            .Include(c => c.Items).ThenInclude(i => i.Variant).ThenInclude(v => v.Product).ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(c => c.UserId == userId, ct);
}

public sealed class OrderRepository(AppDbContext db) : GenericRepository<Order>(db), IOrderRepository
{
    private IQueryable<Order> WithDetails() =>
        Set.Include(o => o.Items).Include(o => o.StatusHistory).Include(o => o.Payments).Include(o => o.User);

    public Task<Order?> GetDetailsAsync(int id, CancellationToken ct = default) =>
        WithDetails().FirstOrDefaultAsync(o => o.Id == id, ct);

    public Task<Order?> GetDetailsByNumberAsync(string orderNumber, CancellationToken ct = default) =>
        WithDetails().FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);

    public async Task<(IReadOnlyList<Order> Items, int Total)> SearchAsync(OrderSearchCriteria c, CancellationToken ct = default)
    {
        var q = Set.AsNoTracking().AsQueryable();
        if (c.UserId is not null) q = q.Where(o => o.UserId == c.UserId);
        if (c.Status is { } s) q = q.Where(o => o.Status == s);
        if (c.PaymentStatus is { } ps) q = q.Where(o => o.PaymentStatus == ps);
        if (c.PaymentMethod is { } pm) q = q.Where(o => o.PaymentMethod == pm);
        if (c.From is { } from) q = q.Where(o => o.CreatedAt >= from);
        if (c.To is { } to) q = q.Where(o => o.CreatedAt < to);
        if (!string.IsNullOrWhiteSpace(c.Search))
        {
            var term = c.Search;
            q = q.Where(o => o.OrderNumber.Contains(term) || o.ShipPhone.Contains(term) || o.ShipRecipientName.Contains(term)
                             || o.User.Email!.Contains(term) || o.User.FullName.Contains(term));
        }

        var total = await q.CountAsync(ct);
        var items = await q.Include(o => o.Items).Include(o => o.User)
            .OrderByDescending(o => o.CreatedAt).ThenByDescending(o => o.Id)
            .Skip(c.Skip).Take(c.Take).ToListAsync(ct);
        return (items, total);
    }

    public Task<bool> HasDeliveredPurchaseAsync(string userId, int productId, CancellationToken ct = default) =>
        Db.OrderItems.AnyAsync(i => i.ProductId == productId && i.Order.UserId == userId && i.Order.Status == OrderStatus.Delivered, ct);
}

public sealed class InventoryRepository(AppDbContext db) : IInventoryRepository
{
    public async Task<(IReadOnlyList<InventoryRow> Items, int Total)> SearchAsync(string? search, bool lowStockOnly, int skip, int take, CancellationToken ct = default)
    {
        var q = db.ProductVariants.AsNoTracking().Where(v => !v.Product.IsDeleted);
        if (lowStockOnly) q = q.Where(v => v.IsActive && v.StockQuantity <= v.LowStockThreshold);
        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(v => v.Sku.Contains(search) || v.Product.NameAr.Contains(search) || v.Product.NameEn.Contains(search));

        var total = await q.CountAsync(ct);
        var items = await q.OrderBy(v => v.StockQuantity).ThenBy(v => v.Id).Skip(skip).Take(take)
            .Select(v => new InventoryRow(v.Id, v.ProductId, v.Product.NameAr, v.Product.NameEn, v.Sku, v.LabelAr, v.LabelEn,
                v.StockQuantity, v.LowStockThreshold, v.IsActive, v.Product.IsActive))
            .ToListAsync(ct);
        return (items, total);
    }

    public async Task<(IReadOnlyList<InventoryTransaction> Items, int Total)> GetTransactionsAsync(int variantId, int skip, int take, CancellationToken ct = default)
    {
        var q = db.InventoryTransactions.AsNoTracking().Where(t => t.VariantId == variantId);
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id).Skip(skip).Take(take).ToListAsync(ct);
        return (items, total);
    }
}
