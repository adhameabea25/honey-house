using HoneyHouse.Domain.Common;

namespace HoneyHouse.Domain.Entities;

/// <summary>A sellable weight option of a product (e.g. 500 g) with its own price and stock.</summary>
public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Sku { get; set; } = string.Empty;
    public int WeightGrams { get; set; }
    public string LabelAr { get; set; } = string.Empty;
    public string LabelEn { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal? CompareAtPrice { get; set; }
    public int StockQuantity { get; set; }
    public int LowStockThreshold { get; set; } = 5;
    public bool IsActive { get; set; } = true;
    public byte[] RowVersion { get; set; } = [];

    public bool IsLowStock => StockQuantity <= LowStockThreshold;
    public bool IsInStock => StockQuantity > 0;
}
