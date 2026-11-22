using HoneyHouse.Domain.Entities;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Application.Interfaces.Repositories;

public interface IAddressRepository : IGenericRepository<Address>
{
    Task<IReadOnlyList<Address>> GetForUserAsync(string userId, CancellationToken ct = default);
    Task<Address?> GetForUserAsync(string userId, int id, CancellationToken ct = default);
}

public interface ICartRepository : IGenericRepository<Cart>
{
    /// <summary>Cart with items, variants, products and images (tracked).</summary>
    Task<Cart?> GetWithItemsAsync(string userId, CancellationToken ct = default);
}

public sealed record OrderSearchCriteria(
    string? UserId, OrderStatus? Status, PaymentStatus? PaymentStatus, PaymentMethod? PaymentMethod,
    DateTime? From, DateTime? To, string? Search, int Skip, int Take);

public interface IOrderRepository : IGenericRepository<Order>
{
    /// <summary>Order with items, status history and payments (tracked).</summary>
    Task<Order?> GetDetailsAsync(int id, CancellationToken ct = default);
    Task<Order?> GetDetailsByNumberAsync(string orderNumber, CancellationToken ct = default);
    Task<(IReadOnlyList<Order> Items, int Total)> SearchAsync(OrderSearchCriteria criteria, CancellationToken ct = default);
    Task<bool> HasDeliveredPurchaseAsync(string userId, int productId, CancellationToken ct = default);
}

public sealed record InventoryRow(int VariantId, int ProductId, string ProductNameAr, string ProductNameEn, string Sku,
    string LabelAr, string LabelEn, int StockQuantity, int LowStockThreshold, bool IsActive, bool ProductIsActive);

public interface IInventoryRepository
{
    Task<(IReadOnlyList<InventoryRow> Items, int Total)> SearchAsync(string? search, bool lowStockOnly, int skip, int take, CancellationToken ct = default);
    Task<(IReadOnlyList<InventoryTransaction> Items, int Total)> GetTransactionsAsync(int variantId, int skip, int take, CancellationToken ct = default);
}
