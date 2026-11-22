using HoneyHouse.Domain.Common;

namespace HoneyHouse.Domain.Entities;

public class Cart : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<CartItem> Items { get; set; } = new List<CartItem>();
}

public class CartItem : BaseEntity
{
    public int CartId { get; set; }
    public Cart Cart { get; set; } = null!;
    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public int Quantity { get; set; }
}
