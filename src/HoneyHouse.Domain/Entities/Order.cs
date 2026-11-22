using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Domain.Entities;

public class Order : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingFee { get; set; }
    public decimal Total { get; set; }

    public int? CouponId { get; set; }
    public Coupon? Coupon { get; set; }
    public string? CouponCode { get; set; }

    // Shipping address snapshot taken at checkout time
    public string ShipRecipientName { get; set; } = string.Empty;
    public string ShipPhone { get; set; } = string.Empty;
    public int ShipGovernorateId { get; set; }
    public string ShipGovernorateName { get; set; } = string.Empty;
    public string ShipCity { get; set; } = string.Empty;
    public string ShipAddressLine { get; set; } = string.Empty;
    public string? ShipLandmark { get; set; }

    public string? CustomerNotes { get; set; }
    public string? TrackingNumber { get; set; }
    public DateTime? CancelledAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? PaymentDueAt { get; set; }

    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<OrderStatusHistory> StatusHistory { get; set; } = new List<OrderStatusHistory>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public int VariantId { get; set; }
    public ProductVariant Variant { get; set; } = null!;
    public int ProductId { get; set; }
    public string ProductNameAr { get; set; } = string.Empty;
    public string ProductNameEn { get; set; } = string.Empty;
    public string VariantLabelAr { get; set; } = string.Empty;
    public string VariantLabelEn { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal LineTotal { get; set; }
}

public class OrderStatusHistory : BaseEntity
{
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public string? ChangedByUserId { get; set; }
}
