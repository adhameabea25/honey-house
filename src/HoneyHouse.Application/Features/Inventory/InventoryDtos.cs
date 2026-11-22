using FluentValidation;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Application.Features.Inventory;

public sealed record InventoryItemDto(int VariantId, int ProductId, string ProductNameAr, string ProductNameEn, string Sku,
    string LabelAr, string LabelEn, int StockQuantity, int LowStockThreshold, bool IsLowStock, bool IsActive, bool ProductIsActive);

public sealed record InventoryTransactionDto(int Id, int QuantityChange, int StockAfter, InventoryTransactionType Type, int? OrderId,
    string? Note, string? CreatedByUserId, DateTime CreatedAt);

public sealed record AdjustStockRequest(int QuantityChange, InventoryTransactionType Type, string? Note);

public sealed record InventoryQuery(string? Search = null, bool LowStockOnly = false, int Page = 1, int PageSize = 50);

public sealed class AdjustStockRequestValidator : AbstractValidator<AdjustStockRequest>
{
    public AdjustStockRequestValidator()
    {
        RuleFor(x => x.QuantityChange).NotEqual(0);
        RuleFor(x => x.Type).Must(t => t is InventoryTransactionType.Restock or InventoryTransactionType.Adjustment)
            .WithErrorCode("inventory_type_invalid");
        RuleFor(x => x.QuantityChange).GreaterThan(0).When(x => x.Type == InventoryTransactionType.Restock);
        RuleFor(x => x.Note).MaximumLength(500);
    }
}
