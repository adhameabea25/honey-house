using HoneyHouse.Admin.Common;
using HoneyHouse.Application.Common;
using HoneyHouse.Application.Features.Addresses;
using HoneyHouse.Application.Features.Coupons;
using HoneyHouse.Application.Features.Reviews;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Admin.Models;

public sealed class CouponListViewModel
{
    public required PagedResult<AdminCouponDto> Result { get; init; }
    public string? Search { get; init; }
    public bool? IsActive { get; init; }
    public DateTime NowUtc { get; init; }
}

public sealed class CouponForm
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string? DescriptionAr { get; set; }
    public string? DescriptionEn { get; set; }
    public DiscountType DiscountType { get; set; } = DiscountType.Percentage;
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscountAmount { get; set; }
    /// <summary>Store-local (Cairo) date and time from a datetime-local input.</summary>
    public DateTime? StartsAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? UsageLimit { get; set; }
    public int? UsageLimitPerUser { get; set; }
    public bool IsActive { get; set; } = true;
    public int UsedCount { get; set; }

    public CouponUpsertRequest ToRequest() => new((Code ?? "").Trim().ToUpperInvariant(), CategoryForm.Blank(DescriptionAr), CategoryForm.Blank(DescriptionEn),
        DiscountType, Value, MinOrderAmount, MaxDiscountAmount, ToUtc(StartsAt), ToUtc(ExpiresAt), UsageLimit, UsageLimitPerUser, IsActive);

    public static CouponForm From(AdminCouponDto c) => new()
    {
        Id = c.Id, Code = c.Code, DescriptionAr = c.DescriptionAr, DescriptionEn = c.DescriptionEn, DiscountType = c.DiscountType,
        Value = c.Value, MinOrderAmount = c.MinOrderAmount, MaxDiscountAmount = c.MaxDiscountAmount,
        StartsAt = c.StartsAt is { } s ? Fmt.ToLocal(s) : null, ExpiresAt = c.ExpiresAt is { } e ? Fmt.ToLocal(e) : null,
        UsageLimit = c.UsageLimit, UsageLimitPerUser = c.UsageLimitPerUser, IsActive = c.IsActive, UsedCount = c.UsedCount
    };

    private static DateTime? ToUtc(DateTime? local) => local is { } l
        ? TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(l, DateTimeKind.Unspecified), HoneyHouse.Application.Features.Analytics.AnalyticsService.StoreTimeZone)
        : null;
}

public sealed class ReviewListViewModel
{
    public required PagedResult<AdminReviewDto> Result { get; init; }
    public string Status { get; init; } = "pending";
}

public sealed class GovernorateRowForm
{
    public decimal ShippingFee { get; set; }
    public bool IsActive { get; set; }
}

public sealed class GovernoratesViewModel
{
    public required IReadOnlyList<AdminGovernorateDto> Items { get; init; }
    public int? ErrorId { get; init; }
}
