using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Application.Interfaces.Repositories;

public sealed record OrderFact(int Id, DateTime CreatedAt, OrderStatus Status, PaymentStatus PaymentStatus, decimal Total, decimal Discount, int ItemCount, string Governorate);
public sealed record ProductSalesRow(int ProductId, string NameAr, string NameEn, int Quantity, decimal Revenue);

public interface IAnalyticsRepository
{
    Task<IReadOnlyList<OrderFact>> GetOrdersAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<IReadOnlyList<ProductSalesRow>> GetTopProductsAsync(DateTime from, DateTime to, int limit, CancellationToken ct = default);
    Task<IReadOnlyDictionary<OrderStatus, int>> CountOrdersByStatusAsync(CancellationToken ct = default);
    Task<int> CountOrdersInStatusAsync(OrderStatus status, CancellationToken ct = default);
    Task<int> CountNewCustomersAsync(DateTime from, DateTime to, CancellationToken ct = default);
    Task<int> CountLowStockVariantsAsync(CancellationToken ct = default);
}
