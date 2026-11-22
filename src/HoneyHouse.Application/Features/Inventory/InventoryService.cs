using HoneyHouse.Application.Common;
using HoneyHouse.Application.Interfaces.Repositories;
using HoneyHouse.Application.Interfaces.Services;
using HoneyHouse.Domain.Entities;

namespace HoneyHouse.Application.Features.Inventory;

public interface IInventoryService
{
    Task<PagedResult<InventoryItemDto>> SearchAsync(InventoryQuery query, CancellationToken ct = default);
    Task<Result<InventoryItemDto>> GetAsync(int variantId, CancellationToken ct = default);
    Task<Result<InventoryItemDto>> AdjustAsync(int variantId, AdjustStockRequest request, CancellationToken ct = default);
    Task<Result<PagedResult<InventoryTransactionDto>>> GetTransactionsAsync(int variantId, int page, int pageSize, CancellationToken ct = default);
}

public sealed class InventoryService(IInventoryRepository inventory, IProductRepository products, IUnitOfWork uow, ICurrentUser currentUser) : IInventoryService
{
    public async Task<PagedResult<InventoryItemDto>> SearchAsync(InventoryQuery q, CancellationToken ct = default)
    {
        var paging = new PagedRequest { Page = q.Page, PageSize = q.PageSize };
        var (items, total) = await inventory.SearchAsync(q.Search?.Trim(), q.LowStockOnly, paging.Skip, paging.PageSize, ct);
        return new PagedResult<InventoryItemDto>(items.Select(ToDto).ToList(), paging.Page, paging.PageSize, total);
    }

    public async Task<Result<InventoryItemDto>> GetAsync(int variantId, CancellationToken ct = default)
    {
        var variant = await products.GetVariantAsync(variantId, ct);
        return variant is null ? Error.NotFound("variant_not_found") : ToDto(ToRow(variant));
    }

    public async Task<Result<InventoryItemDto>> AdjustAsync(int variantId, AdjustStockRequest r, CancellationToken ct = default)
    {
        var variant = await products.GetVariantAsync(variantId, ct);
        if (variant is null) return Error.NotFound("variant_not_found");
        if (variant.StockQuantity + r.QuantityChange < 0) return Error.Business("stock_negative", variant.StockQuantity);

        variant.StockQuantity += r.QuantityChange;
        await uow.Repository<InventoryTransaction>().AddAsync(new InventoryTransaction
        {
            VariantId = variant.Id, QuantityChange = r.QuantityChange, StockAfter = variant.StockQuantity,
            Type = r.Type, Note = r.Note?.Trim(), CreatedByUserId = currentUser.UserId
        }, ct);
        await uow.SaveChangesAsync(ct);

        return ToDto(ToRow(variant));
    }

    public async Task<Result<PagedResult<InventoryTransactionDto>>> GetTransactionsAsync(int variantId, int page, int pageSize, CancellationToken ct = default)
    {
        if (await products.GetVariantAsync(variantId, ct) is null) return Error.NotFound("variant_not_found");
        var paging = new PagedRequest { Page = page, PageSize = pageSize };
        var (items, total) = await inventory.GetTransactionsAsync(variantId, paging.Skip, paging.PageSize, ct);
        return new PagedResult<InventoryTransactionDto>(items.Select(t => new InventoryTransactionDto(
            t.Id, t.QuantityChange, t.StockAfter, t.Type, t.OrderId, t.Note, t.CreatedByUserId, t.CreatedAt)).ToList(),
            paging.Page, paging.PageSize, total);
    }

    private static InventoryRow ToRow(ProductVariant v) => new(v.Id, v.ProductId, v.Product.NameAr, v.Product.NameEn, v.Sku,
        v.LabelAr, v.LabelEn, v.StockQuantity, v.LowStockThreshold, v.IsActive, v.Product.IsActive);

    private static InventoryItemDto ToDto(InventoryRow r) => new(r.VariantId, r.ProductId, r.ProductNameAr, r.ProductNameEn, r.Sku,
        r.LabelAr, r.LabelEn, r.StockQuantity, r.LowStockThreshold, r.StockQuantity <= r.LowStockThreshold, r.IsActive, r.ProductIsActive);
}
