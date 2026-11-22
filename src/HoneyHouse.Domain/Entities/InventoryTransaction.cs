using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Domain.Entities;

public class InventoryTransaction : BaseEntity
{
    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public int QuantityChange { get; set; }
    public int StockAfter { get; set; }
    public InventoryTransactionType Type { get; set; }
    public int? OrderId { get; set; }
    public string? Note { get; set; }
    public string? CreatedByUserId { get; set; }
}
