using HoneyHouse.Domain.Common;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Domain.Entities;

public class Coupon : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? UsageLimit { get; set; }
    public int? UsageLimitPerUser { get; set; }
    public int UsedCount { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();

    /// <summary>Discount for a given subtotal, never exceeding the subtotal or MaxDiscountAmount.</summary>
    public decimal CalculateDiscount(decimal subtotal)
    {
        if (subtotal <= 0) return 0;
        var discount = DiscountType == DiscountType.Percentage
            ? Math.Round(subtotal * Value / 100m, 2, MidpointRounding.AwayFromZero)
            : Value;
        if (MaxDiscountAmount is { } max && discount > max) discount = max;
        return Math.Min(discount, subtotal);
    }
}

public class CouponUsage : BaseEntity
{
    public int CouponId { get; set; }
    public Coupon Coupon { get; set; } = null!;
    public string UserId { get; set; } = string.Empty;
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    public decimal DiscountAmount { get; set; }
}
