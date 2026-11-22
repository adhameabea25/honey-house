using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Inventory;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Admin.Models;

public sealed class InventoryListViewModel
{
    public required PagedResult<InventoryItemDto> Result { get; init; }
    public string? Search { get; init; }
    public bool LowStockOnly { get; init; }
}

public sealed class StockAdjustForm
{
    public InventoryTransactionType Type { get; set; } = InventoryTransactionType.Restock;
    public int QuantityChange { get; set; }
    public string? Note { get; set; }

    public AdjustStockRequest ToRequest() => new(QuantityChange, Type, string.IsNullOrWhiteSpace(Note) ? null : Note.Trim());
}

public sealed class InventoryVariantViewModel
{
    public required InventoryItemDto Item { get; init; }
    public required PagedResult<InventoryTransactionDto> History { get; init; }
    public IReadOnlyDictionary<string, string> UserNames { get; init; } = new Dictionary<string, string>();
    public IReadOnlyDictionary<int, string> OrderNumbers { get; init; } = new Dictionary<int, string>();
    public StockAdjustForm Form { get; set; } = new();
}
