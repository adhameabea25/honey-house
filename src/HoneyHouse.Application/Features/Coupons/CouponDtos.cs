using FluentValidation;
using HoneyHouse.Domain.Enums;

namespace HoneyHouse.Application.Features.Coupons;

public sealed record ValidateCouponRequest(string Code);

public sealed record CouponValidationDto(string Code, string? Description, decimal Subtotal, decimal DiscountAmount, decimal SubtotalAfterDiscount, string Currency);

public sealed record CouponUpsertRequest(
    string Code, string? DescriptionAr, string? DescriptionEn, DiscountType DiscountType, decimal Value,
    decimal? MinOrderAmount, decimal? MaxDiscountAmount, DateTime? StartsAt, DateTime? ExpiresAt,
    int? UsageLimit, int? UsageLimitPerUser, bool IsActive = true);

public sealed record AdminCouponDto(
    int Id, string Code, string? DescriptionAr, string? DescriptionEn, DiscountType DiscountType, decimal Value,
    decimal? MinOrderAmount, decimal? MaxDiscountAmount, DateTime? StartsAt, DateTime? ExpiresAt,
    int? UsageLimit, int? UsageLimitPerUser, int UsedCount, bool IsActive, DateTime CreatedAt);

public sealed record CouponQuery(string? Search = null, bool? IsActive = null, int Page = 1, int PageSize = 20);

public sealed class ValidateCouponRequestValidator : AbstractValidator<ValidateCouponRequest>
{
    public ValidateCouponRequestValidator() => RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
}

public sealed class CouponUpsertRequestValidator : AbstractValidator<CouponUpsertRequest>
{
    public CouponUpsertRequestValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50).Matches("^[A-Za-z0-9_-]+$");
        RuleFor(x => x.DescriptionAr).MaximumLength(500);
        RuleFor(x => x.DescriptionEn).MaximumLength(500);
        RuleFor(x => x.DiscountType).IsInEnum();
        RuleFor(x => x.Value).GreaterThan(0).PrecisionScale(18, 2, true);
        RuleFor(x => x.Value).LessThanOrEqualTo(100).When(x => x.DiscountType == DiscountType.Percentage);
        RuleFor(x => x.MinOrderAmount).GreaterThanOrEqualTo(0).When(x => x.MinOrderAmount.HasValue);
        RuleFor(x => x.MaxDiscountAmount).GreaterThan(0).When(x => x.MaxDiscountAmount.HasValue);
        RuleFor(x => x.ExpiresAt).GreaterThan(x => x.StartsAt).When(x => x.StartsAt.HasValue && x.ExpiresAt.HasValue);
        RuleFor(x => x.UsageLimit).GreaterThan(0).When(x => x.UsageLimit.HasValue);
        RuleFor(x => x.UsageLimitPerUser).GreaterThan(0).When(x => x.UsageLimitPerUser.HasValue);
    }
}
